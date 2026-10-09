using DDCSharp.Core.Abstractions;
using DDCSharp.Core.Capabilities;

namespace DDCSharp.Linux;

/// <summary>Display controlled with DDC/CI over a <c>/dev/i2c-N</c> bus.</summary>
internal sealed class LinuxDisplay : DDCDisplay
{
    private static readonly TimeSpan InputSwitchAcknowledgeWait = TimeSpan.FromMilliseconds(500);

    private readonly DDCCIChannel _channel;

    public LinuxDisplay(DDCCIChannel channel, string id, string description, string connector)
        : base(id, description)
    {
        _channel = channel;
        Connector = connector;
    }

    /// <summary>DRM connector name, e.g. <c>DP-4</c>.</summary>
    public string Connector { get; }

    public override bool TryGetVCPFeature(byte code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
    {
        var status = _channel.TryGetVCP(code, out var reading, out var error);
        type = reading.Type;
        currentValue = reading.Current;
        maximumValue = reading.Maximum;
        return status == DDCStatus.Ok ? Succeed() : Fail(error ?? "Unknown error");
    }

    public override bool TrySetVCPFeature(byte code, uint value)
    {
        if (value > ushort.MaxValue)
        {
            return Fail($"Value {value} does not fit in the 16-bit DDC/CI value field");
        }

        // Switching away from the input this machine is on tears down the link right after the display accepts
        // the command, so the acknowledgement fails (over MST only after a 4 second kernel timeout) although the
        // switch happened. Input switches are therefore sent once and not waited for.
        var written = code == (byte)VCPFeature.InputSource
            ? _channel.TrySetVCPInBackground(code, (ushort)value, InputSwitchAcknowledgeWait, out var error)
            : _channel.TrySetVCP(code, (ushort)value, out error);
        return written ? Succeed() : Fail(error!);
    }

    protected override string? ReadCapabilitiesString()
    {
        var capabilities = _channel.TryGetCapabilities(out var error);
        if (capabilities == null)
        {
            Fail(error ?? "Unknown error");
            return null;
        }

        Succeed();
        return capabilities;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _channel.Dispose();
        }
    }

    public override string ToString() => $"{Description} [{Id}] on {Connector} via {_channel.BusPath}";
}

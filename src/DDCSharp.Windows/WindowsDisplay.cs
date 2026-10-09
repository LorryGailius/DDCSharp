using System.Runtime.InteropServices;
using System.Text;
using DDCSharp.Core.Abstractions;
using DDCSharp.Core.Capabilities;

namespace DDCSharp.Windows;

/// <summary>Display controlled through the DXVA2 monitor configuration API.</summary>
internal sealed class WindowsDisplay : DDCDisplay
{
    private readonly PhysicalMonitorHandle _handle;
    private readonly object _sync = new();

    public WindowsDisplay(PhysicalMonitorHandle handle, string id, string description, string deviceName)
        : base(id, description)
    {
        _handle = handle;
        DeviceName = deviceName;
    }

    /// <summary>GDI device name of the logical monitor, e.g. <c>\\.\DISPLAY1</c>.</summary>
    public string DeviceName { get; }

    /// <summary>
    /// Checks whether the monitor answers DDC/CI. A "VCP code not supported" reply still proves
    /// that DDC/CI works, which is much cheaper to find out than fetching the capabilities string.
    /// </summary>
    public static bool Probe(PhysicalMonitorHandle handle, out string? error)
    {
        if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(handle, (byte)VCPFeature.Brightness, out _, out _, out _))
        {
            error = null;
            return true;
        }

        var code = Marshal.GetLastPInvokeError();
        error = NativeMethods.DescribeError(code);
        return code == NativeMethods.ErrorGraphicsDDCCIVCPNotSupported;
    }

    public override bool TryGetVCPFeature(byte code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
    {
        lock (_sync)
        {
            if (!NativeMethods.GetVCPFeatureAndVCPFeatureReply(
                    _handle,
                    code,
                    out var codeType,
                    out currentValue,
                    out maximumValue))
            {
                type = default;
                return Fail(NativeMethods.DescribeError(Marshal.GetLastPInvokeError()));
            }

            type = codeType == NativeMethods.MCMomentary ? VCPFeatureType.Momentary : VCPFeatureType.SetParameter;
            return Succeed();
        }
    }

    public override bool TrySetVCPFeature(byte code, uint value)
    {
        lock (_sync)
        {
            return NativeMethods.SetVCPFeature(_handle, code, value)
                ? Succeed()
                : Fail(NativeMethods.DescribeError(Marshal.GetLastPInvokeError()));
        }
    }

    protected override unsafe string? ReadCapabilitiesString()
    {
        lock (_sync)
        {
            // Both calls run the full DDC/CI capabilities exchange, so this takes roughly twice as long as one read
            if (!NativeMethods.GetCapabilitiesStringLength(_handle, out var length))
            {
                Fail(NativeMethods.DescribeError(Marshal.GetLastPInvokeError()));
                return null;
            }
            if (length == 0)
            {
                Fail("Display returned an empty capabilities string");
                return null;
            }

            var buffer = new byte[length];
            fixed (byte* pointer = buffer)
            {
                if (!NativeMethods.CapabilitiesRequestAndCapabilitiesReply(_handle, pointer, length))
                {
                    Fail(NativeMethods.DescribeError(Marshal.GetLastPInvokeError()));
                    return null;
                }
            }

            Succeed();
            var end = Array.IndexOf(buffer, (byte)0);
            return Encoding.ASCII.GetString(buffer, 0, end < 0 ? buffer.Length : end);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _handle.Dispose();
        }
    }

    public override string ToString() => $"{Description} [{Id}] on {DeviceName}";
}

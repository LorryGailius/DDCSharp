using DDCSharp.Core.Abstractions;
using DDCSharp.Core.Capabilities;

namespace DDCSharp.Windows;

internal sealed class WindowsDisplay : IDisplay
{
    private readonly WindowsDisplayHandle _handle;
    private readonly object _sync = new();
    private IReadOnlyCollection<Capability> _capabilities = [];

    public WindowsDisplay(WindowsDisplayHandle handle, WindowsDisplayInfo info)
    {
        _handle = handle;
        ApplyInfo(info);
    }

    private void ApplyInfo(WindowsDisplayInfo info)
    {
        Description = info.Description;
        Type = info.Type;
        Model = info.Model;
        MCCSVersion = info.MccsVersion;
        _capabilities = info.Capabilities;
    }

    public string Description { get; private set; } = string.Empty;
    public string? Type { get; private set; }
    public string? Model { get; private set; }
    public Version? MCCSVersion { get; private set; }
    public IReadOnlyCollection<Capability> Capabilities => _capabilities;

    public bool TryGetVcpFeature(VCPFeature code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
    {
        lock (_sync)
        {
            if (!WinAPI.GetVCPFeatureAndVCPFeatureReply(_handle.Handle, (byte)code, out var nativeType, out var current, out var max))
            {
                type = default;
                currentValue = 0;
                maximumValue = 0;
                return false;
            }
            type = (VCPFeatureType)nativeType;
            currentValue = current;
            maximumValue = max;
            return true;
        }
    }

    public bool TrySetVcpFeature(VCPFeature code, uint value)
    {
        lock (_sync)
        {
            return WinAPI.SetVCPFeature(_handle.Handle, (byte)code, value);
        }
    }

    public IReadOnlyCollection<InputSource> GetSupportedInputSources()
    {
        var displayCapability = _capabilities.FirstOrDefault(c => c.Feature == VCPFeature.InputSource);
        if (displayCapability == null)
            return [];

        return displayCapability.SupportedValues
            .Select(v => Enum.IsDefined(typeof(InputSource), v) ? (InputSource)v : InputSource.Unknown)
            .ToList();
    }

    public bool TrySetInputSource(InputSource input)
    {
        if (!GetSupportedInputSources().Any(x => x == input))
        {
            return false;
        }
        return TrySetVcpFeature(VCPFeature.InputSource, (byte)input);
    }

    public bool TrySetBrightness(uint brightness)
    {
        var capability = _capabilities.FirstOrDefault(c => c.Feature == VCPFeature.Brightness);
        if (capability == null)
        {
            return false;
        }

        return TrySetVcpFeature(VCPFeature.Brightness, brightness);
    }

    public void RefreshCapabilities()
    {
        lock (_sync)
        {
            var info = WindowsDisplayInfo.Create(_handle);
            ApplyInfo(info);
        }
    }

    public void Dispose() => _handle.Dispose();
}

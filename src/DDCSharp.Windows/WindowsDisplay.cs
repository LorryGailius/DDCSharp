using System.Diagnostics;
using DDCSharp.Core;
using DDCSharp.Core.Abstractions;
using DDCSharp.Core.Capabilities;

namespace DDCSharp.Windows;

internal sealed class WindowsDisplay : IDisplay
{
    private WindowsDisplayHandle _handle;
    private readonly object _sync = new();

    public WindowsDisplay(WindowsDisplayHandle handle, WindowsDisplayInfo info)
    {
        _handle = handle;
        Id = handle.Id;
        ApplyInfo(info);
    }

    private void ApplyInfo(WindowsDisplayInfo info)
    {
        Description = info.Description;
        Type = info.Type;
        Model = info.Model;
        MCCSVersion = info.MccsVersion;
        Capabilities = info.Capabilities;
        SupportsVCP = info.SupportsVCP;
    }

    public string Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Type { get; private set; }
    public string? Model { get; private set; }
    public Version? MCCSVersion { get; private set; }
    public IReadOnlyCollection<Capability> Capabilities { get; private set; } = [];
    public bool SupportsVCP { get; private set; }

    public bool TryGetVCPFeature(byte code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
    {
        lock (_sync)
        {
            if (!WinAPI.GetVCPFeatureAndVCPFeatureReply(
                    _handle.Handle,
                    code,
                    out var nativeType, 
                    out var current,
                    out var max))
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

    public bool TrySetVCPFeature(byte code, uint value)
    {
        lock (_sync)
        {
            return WinAPI.SetVCPFeature(_handle.Handle, code, value);
        }
    }

    public IReadOnlyCollection<InputSource> GetSupportedInputSources()
    {
        var displayCapability = Capabilities.FirstOrDefault(c => c.Feature == VCPFeature.InputSource);
        if (displayCapability == null)
        {
            return [];
        }

        return displayCapability.SupportedValues
            .Select(v => Enum.IsDefined(typeof(InputSource), v) ? (InputSource)v : InputSource.Unknown)
            .ToList();
    }

    public bool TrySetInputSource(InputSource targetInput, TimeSpan? timeout = null)
    {
        if (!GetSupportedInputSources().Contains(targetInput))
        {
            return false;
        }

        TrySetVCPFeature((byte)VCPFeature.InputSource, (byte)targetInput);

        ReAttachHandle();

        var pollInterval = TimeSpan.FromTicks(1);

        if (timeout == null || timeout <= TimeSpan.Zero)
        {
            return GetInputSource() == targetInput;
        }

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout.Value)
        {
            if (GetInputSource() == targetInput)
            {
                return true;
            }

            var remaining = timeout.Value - sw.Elapsed;
            var delay = remaining < pollInterval
                ? remaining
                : pollInterval;

            if (delay > TimeSpan.Zero)
            {
                Thread.Sleep(delay);
            }
        }

        return GetInputSource() == targetInput;
    }

    public async Task<bool> TrySetInputSourceAsync(
        InputSource targetInput,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!GetSupportedInputSources().Contains(targetInput))
        {
            return false;
        }

        TrySetVCPFeature((byte)VCPFeature.InputSource, (byte)targetInput);

        ReAttachHandle();

        var appliedTimeout = timeout ?? TimeSpan.FromSeconds(5);
        var pollInterval = TimeSpan.FromTicks(1);

        if (appliedTimeout <= TimeSpan.Zero)
        {
            return GetInputSource() == targetInput;
        }

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed <= appliedTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (GetInputSource() == targetInput)
            {
                return true;
            }

            var remaining = appliedTimeout - sw.Elapsed;
            var delay = remaining < pollInterval
                ? remaining
                : pollInterval;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        return GetInputSource() == targetInput;
    }

    public InputSource GetInputSource()
    {
        if (!TryGetVCPFeature((byte)VCPFeature.InputSource, out _, out var currentValue, out _))
        {
            return InputSource.Unknown;
        }
        return Enum.IsDefined(typeof(InputSource), (byte)currentValue) ? (InputSource)(byte)currentValue : InputSource.Unknown;
    }

    public bool TrySetBrightness(uint brightness)
    {
        var capability = Capabilities.FirstOrDefault(c => c.Feature == VCPFeature.Brightness);
        if (capability == null)
        {
            return false;
        }

        return TrySetVCPFeature((byte)VCPFeature.Brightness, brightness);
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

    private void ReAttachHandle()
    {
        lock (_sync)
        {
            var newHandle = WindowsDisplayProvider.GetHandleById(Id);
            if (newHandle == null || newHandle.Handle == _handle.Handle)
            {
                return;
            }

            _handle.Dispose();
            _handle = newHandle;
        }
    }
}

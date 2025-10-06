using DDCSharp.Core.Abstractions;

namespace DDCSharp.Windows;

public sealed class WindowsDisplayHandle : IDisplayHandle
{
    public string Id { get; }
    public nint Handle { get; }
    public string Description { get; }
    private bool _disposed;

    internal WindowsDisplayHandle(nint hMonitor, WinAPI.PHYSICAL_MONITOR physical)
    {
        Handle = physical.hPhysicalMonitor;
        Description = physical.szPhysicalMonitorDescription.Trim();
        var devName = WinAPI.GetMonitorDeviceName(hMonitor);
        Id = string.IsNullOrWhiteSpace(devName)
            ? $"{Description}"
            : $"{devName}|{Description}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        WinAPI.DestroyPhysicalMonitor(Handle);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~WindowsDisplayHandle()
    {
        if (!_disposed)
        {
            WinAPI.DestroyPhysicalMonitor(Handle);
        }
    }
}

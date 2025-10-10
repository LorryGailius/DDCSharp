namespace DDCSharp.Windows;

internal sealed class WindowsDisplayHandle : IDisposable
{
    public nint Handle { get; }
    public string Description { get; }
    public string? DeviceId { get; }
    private bool _disposed;

    internal WindowsDisplayHandle(WinAPI.PHYSICAL_MONITOR physical, string? deviceId = null)
    {
        Handle = physical.hPhysicalMonitor;
        Description = physical.szPhysicalMonitorDescription.Trim();
        DeviceId = deviceId;
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
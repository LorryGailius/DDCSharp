namespace DDCSharp.Windows;

internal sealed class WindowsDisplayHandle : IDisposable
{
    public nint Handle { get; }
    public string Description { get; }
    private bool _disposed;

    internal WindowsDisplayHandle(WinAPI.PHYSICAL_MONITOR physical)
    {
        Handle = physical.hPhysicalMonitor;
        Description = physical.szPhysicalMonitorDescription.Trim();
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

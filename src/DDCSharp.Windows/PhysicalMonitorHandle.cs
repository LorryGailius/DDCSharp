using System.Runtime.InteropServices;

namespace DDCSharp.Windows;

/// <summary>DXVA2 physical monitor handle, destroyed with <c>DestroyPhysicalMonitor</c>.</summary>
internal sealed class PhysicalMonitorHandle : SafeHandle
{
    // DXVA2 does not document 0 as invalid, so only -1 marks an unset handle
    public PhysicalMonitorHandle() : base(-1, ownsHandle: true)
    {
    }

    public PhysicalMonitorHandle(nint handle) : this()
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == -1;

    protected override bool ReleaseHandle() => NativeMethods.DestroyPhysicalMonitor(handle);
}

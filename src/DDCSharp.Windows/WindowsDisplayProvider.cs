using DDCSharp.Core;
using DDCSharp.Core.Abstractions;

namespace DDCSharp.Windows;

public sealed class WindowsDisplayProvider : IDisplayProvider
{
    public IEnumerable<IDisplay> GetDisplays()
    {
        foreach (var (hMonitor, deviceInterfaceId) in WinAPI.EnumerateMonitorHandlesWithInterfaceIds())
        {
            var physicals = WinAPI.GetPhysicalMonitors(hMonitor);
            foreach (var p in physicals)
            {
                var handle = new WindowsDisplayHandle(p, deviceInterfaceId);
                var info = WindowsDisplayInfo.Create(handle);
                if (info.SupportsVCP)
                {
                    yield return new WindowsDisplay(handle, info);
                }
                else
                {
                    yield return new NoOpDisplay(info.DeviceId, info.Description, info.Type, info.Model, info.MCCSVersion);
                    handle.Dispose(); // release handle not needed further
                }
            }
        }
    }

    public static void Register() => DisplayService.RegisterProvider(new WindowsDisplayProvider());
}

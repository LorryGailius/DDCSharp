using DDCSharp.Core;
using DDCSharp.Core.Abstractions;

namespace DDCSharp.Windows;

public sealed class WindowsDisplayProvider : IDisplayProvider
{
    public IEnumerable<IDisplay> GetDisplays()
    {
        foreach (var h in WinAPI.EnumerateHMonitors())
        {
            var physicals = WinAPI.GetPhysicalMonitors(h);
            foreach (var p in physicals)
            {
                var handle = new WindowsDisplayHandle(p);
                var info = WindowsDisplayInfo.Create(handle);
                yield return new WindowsDisplay(handle, info);
            }
        }
    }

    public static void Register() => DisplayService.RegisterProvider(new WindowsDisplayProvider());
}

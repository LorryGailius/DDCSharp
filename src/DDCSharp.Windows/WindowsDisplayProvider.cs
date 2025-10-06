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
                var handle = new WindowsDisplayHandle(h, p);
                var info = WindowsDisplayInfo.Create(handle);
                if (info.SupportsVCP)
                {
                    yield return new WindowsDisplay(handle, info);
                }
                else
                {
                    yield return new NoOpDisplay(info.Description, info.Type, info.Model, info.MccsVersion);
                    handle.Dispose(); // release handle not needed further
                }
            }
        }
    }

    public static WindowsDisplayHandle? GetHandleById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (var h in WinAPI.EnumerateHMonitors())
        {
            var physicals = WinAPI.GetPhysicalMonitors(h);
            foreach (var p in physicals)
            {
                var temp = new WindowsDisplayHandle(h, p);
                if (string.Equals(temp.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return temp;
                }
                temp.Dispose();
            }
        }
        return null;
    }

    public static void Register() => DisplayService.RegisterProvider(new WindowsDisplayProvider());
}

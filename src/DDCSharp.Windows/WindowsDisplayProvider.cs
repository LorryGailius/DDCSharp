using System.Security;
using DDCSharp.Core;
using DDCSharp.Core.Abstractions;
using Microsoft.Win32;

namespace DDCSharp.Windows;

/// <summary>
/// Enumerates monitors through the Win32 display APIs and controls them with DXVA2.
/// </summary>
public sealed class WindowsDisplayProvider : IDisplayProvider
{
    /// <inheritdoc />
    public IEnumerable<IDisplay> GetDisplays()
    {
        var displays = new List<IDisplay>();
        foreach (var monitor in NativeMethods.EnumerateMonitors())
        {
            var deviceName = NativeMethods.GetMonitorDeviceName(monitor) ?? string.Empty;
            var interfacePaths = deviceName.Length > 0 ? NativeMethods.GetMonitorInterfacePaths(deviceName) : [];
            var physicalMonitors = NativeMethods.GetPhysicalMonitors(monitor)
                .Select(m => (Handle: new PhysicalMonitorHandle(m.Handle), m.Description))
                .ToList();

            // Physical monitors of one logical monitor are listed in the same order as its active display devices
            for (var i = 0; i < physicalMonitors.Count; i++)
            {
                var (handle, physicalDescription) = physicalMonitors[i];
                var interfacePath = i < interfacePaths.Count ? interfacePaths[i] : null;
                var location = interfacePath ?? $"{deviceName}#{i}";
                var edid = interfacePath != null ? ReadEdid(interfacePath) : null;
                var id = edid?.ToDisplayId(location) ?? location;
                var description = edid?.Name ?? physicalDescription;

                if (WindowsDisplay.Probe(handle, out var error))
                {
                    displays.Add(new WindowsDisplay(handle, id, description, deviceName));
                }
                else
                {
                    handle.Dispose();
                    displays.Add(new NoOpDisplay(id, description, error));
                }
            }
        }
        return displays;
    }

    /// <summary>Registers a <see cref="WindowsDisplayProvider"/> with <see cref="DisplayService"/>.</summary>
    public static void Register() => DisplayService.RegisterProvider(new WindowsDisplayProvider());

    /// <summary>Reads the EDID that Windows stores in the registry for a monitor device.</summary>
    private static EdidInfo? ReadEdid(string interfacePath)
    {
        // \\?\DISPLAY#DELF167#5&2a8b4c3e&0&UID4352#{guid} -> DISPLAY\DELF167\5&2a8b4c3e&0&UID4352
        var path = interfacePath.StartsWith(@"\\?\", StringComparison.Ordinal) ? interfacePath[4..] : interfacePath;
        var guidStart = path.LastIndexOf("#{", StringComparison.Ordinal);
        if (guidStart < 0)
        {
            return null;
        }
        var instancePath = path[..guidStart].Replace('#', '\\');

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{instancePath}\Device Parameters");
            return key?.GetValue("EDID") is byte[] data && EdidInfo.TryParse(data, out var edid) ? edid : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return null;
        }
    }
}

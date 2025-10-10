using DDCSharp.Core.Abstractions;

namespace DDCSharp.Core;

/// <summary>
/// Contract for a display provider (platform specific or virtual).
/// </summary>
public interface IDisplayProvider
{
    IEnumerable<IDisplay> GetDisplays();
}

/// <summary>
/// Static display service that aggregates displays from registered providers.
/// </summary>
public static class DisplayService
{
    private static readonly List<IDisplayProvider> _providers = [];
    private static readonly object _lock = new();

    /// <summary>
    /// Registers a display provider. Thread-safe.
    /// </summary>
    public static void RegisterProvider(IDisplayProvider provider)
    {
        if (provider == null) return;
        lock (_lock)
        {
            _providers.Add(provider);
        }
    }

    /// <summary>
    /// Removes all previously registered providers.
    /// </summary>
    public static void ClearProviders()
    {
        lock (_lock)
        {
            _providers.Clear();
        }
    }

    /// <summary>
    /// Returns all discovered displays (including those without VCP / DDC/CI support).
    /// Provider exceptions are swallowed to avoid blocking enumeration.
    /// </summary>
    public static IReadOnlyList<IDisplay> GetAllDisplays()
    {
        List<IDisplayProvider> snapshot;
        lock (_lock)
        {
            snapshot = _providers.ToList();
        }
        var all = new List<IDisplay>();
        foreach (var p in snapshot)
        {
            try
            {
                var displays = p.GetDisplays();
                if (displays == null) continue;
                all.AddRange(displays);
            }
            catch
            {
                // ignore individual provider failures
            }
        }
        return all;
    }

    /// <summary>
    /// Returns only VCP-capable (controllable) displays. Displays that do not expose
    /// DDC/CI are filtered out (e.g. NoOpDisplay instances).
    /// </summary>
    public static IReadOnlyList<IDisplay> GetDisplays()
    {
        return GetAllDisplays()
            .Where(x => x.SupportsVCP)
            .ToList();
    }

    /// <summary>
    /// Helper to find a display by its device interface id (if provided by platform).
    /// </summary>
    public static IDisplay? FindByDeviceId(string deviceId)
        => GetAllDisplays().FirstOrDefault(d => string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
}

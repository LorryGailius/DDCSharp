namespace DDCSharp.Linux;

/// <summary>A connected DRM connector from <c>/sys/class/drm</c>, e.g. <c>card1-DP-4</c>.</summary>
internal sealed record DRMConnector(string Name, string ConnectorName, string CardDevicePath, byte[] EDID, string? BusPath)
{
    private const string DRMClassPath = "/sys/class/drm";

    private static readonly string[] InternalPanelPrefixes = ["eDP", "LVDS", "DSI"];

    public bool IsInternalPanel => InternalPanelPrefixes.Any(p => ConnectorName.StartsWith(p, StringComparison.Ordinal));

    public static List<DRMConnector> EnumerateConnected()
    {
        var connectors = new List<DRMConnector>();
        if (!Directory.Exists(DRMClassPath))
        {
            return connectors;
        }

        foreach (var directory in Directory.EnumerateDirectories(DRMClassPath, "card*-*").Order(StringComparer.Ordinal))
        {
            if (SysFs.ReadText(Path.Combine(directory, "status")) != "connected")
            {
                continue;
            }

            var name = Path.GetFileName(directory);
            var separator = name.IndexOf('-');
            var cardName = name[..separator];
            connectors.Add(new DRMConnector(
                name,
                name[(separator + 1)..],
                SysFs.ResolvePath(Path.Combine(DRMClassPath, cardName, "device")),
                SysFs.ReadBytes(Path.Combine(directory, "edid")),
                FindDirectBus(directory)));
        }
        return connectors;
    }

    /// <summary>
    /// Finds the I2C bus the kernel links to the connector. DisplayPort MST connectors have none,
    /// their bus has to be found by matching EDIDs.
    /// </summary>
    private static string? FindDirectBus(string connectorDirectory)
    {
        var ddcLink = Path.Combine(connectorDirectory, "ddc");
        var busName = Directory.Exists(ddcLink)
            ? Path.GetFileName(SysFs.ResolvePath(ddcLink))
            : Directory.EnumerateDirectories(connectorDirectory, "i2c-*").Select(Path.GetFileName).FirstOrDefault();

        if (busName == null || !SysFs.IsI2CBusName(busName))
        {
            return null;
        }
        var devicePath = $"/dev/{busName}";
        return File.Exists(devicePath) ? devicePath : null;
    }
}

internal static class SysFs
{
    private const string I2CDevicesPath = "/sys/bus/i2c/devices";

    public static string? ReadText(string path)
    {
        try
        {
            return File.ReadAllText(path).Trim();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static byte[] ReadBytes(string path)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Resolves all symbolic links in a sysfs path. Sysfs links are relative to directories that are links themselves, so this needs realpath(3).</summary>
    public static string ResolvePath(string path) => NativeMethods.ResolvePath(path) ?? path;

    public static bool IsI2CBusName(string name) =>
        name.StartsWith("i2c-", StringComparison.Ordinal) && name.Length > 4 && name[4..].All(char.IsAsciiDigit);

    /// <summary>
    /// Lists I2C buses that belong to a GPU (their sysfs device sits below <paramref name="cardDevicePath"/>),
    /// as (device path, adapter name) pairs. Buses of other devices, such as SMBus with RAM SPD EEPROMs, are never touched.
    /// </summary>
    public static List<(string DevicePath, string Name)> GetGPUBuses(string cardDevicePath)
    {
        var buses = new List<(string, string)>();
        if (!Directory.Exists(I2CDevicesPath))
        {
            return buses;
        }

        foreach (var link in Directory.EnumerateDirectories(I2CDevicesPath, "i2c-*"))
        {
            var busName = Path.GetFileName(link);
            if (!IsI2CBusName(busName) || !ResolvePath(link).StartsWith(cardDevicePath + "/", StringComparison.Ordinal))
            {
                continue;
            }
            var devicePath = $"/dev/{busName}";
            if (File.Exists(devicePath))
            {
                buses.Add((devicePath, ReadText(Path.Combine(link, "name")) ?? string.Empty));
            }
        }
        return buses;
    }
}

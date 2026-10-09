using DDCSharp.Core;
using DDCSharp.Core.Abstractions;
using DDCSharp.Core.Capabilities;

namespace DDCSharp.Linux;

/// <summary>
/// Enumerates monitors from the kernel DRM subsystem and controls them with DDC/CI over <c>/dev/i2c-*</c>.
/// Requires the <c>i2c-dev</c> kernel module and read/write access to the I2C device files.
/// </summary>
public sealed class LinuxDisplayProvider : IDisplayProvider
{
    private readonly DDCTimings _timings;

    /// <param name="timings">DDC/CI delays and retries, <see cref="DDCTimings.Default"/> if null.</param>
    public LinuxDisplayProvider(DDCTimings? timings = null)
    {
        _timings = timings ?? DDCTimings.Default;
    }

    /// <inheritdoc />
    public IEnumerable<IDisplay> GetDisplays()
    {
        var connectors = DRMConnector.EnumerateConnected();
        var usedBuses = connectors
            .Where(c => c.BusPath != null)
            .Select(c => c.BusPath!)
            .ToHashSet(StringComparer.Ordinal);

        var displays = new List<IDisplay>();
        foreach (var connector in connectors)
        {
            var edid = EDIDInfo.TryParse(connector.EDID, out var parsed) ? parsed : null;
            var id = edid?.ToDisplayId(connector.ConnectorName) ?? connector.Name;
            var description = edid?.Name ?? connector.ConnectorName;

            if (connector.IsInternalPanel)
            {
                displays.Add(new NoOpDisplay(id, description, "Internal laptop panels do not support DDC/CI"));
                continue;
            }

            var busPath = connector.BusPath ?? FindBusByEDID(connector, usedBuses);
            if (busPath == null)
            {
                displays.Add(new NoOpDisplay(id, description, $"No I2C bus found for connector {connector.ConnectorName}"));
                continue;
            }

            usedBuses.Add(busPath);
            displays.Add(CreateDisplay(busPath, id, description, connector.ConnectorName));
        }
        return displays;
    }

    /// <summary>Registers a <see cref="LinuxDisplayProvider"/> with <see cref="DisplayService"/>.</summary>
    public static void Register(DDCTimings? timings = null) =>
        DisplayService.RegisterProvider(new LinuxDisplayProvider(timings));

    private IDisplay CreateDisplay(string busPath, string id, string description, string connector)
    {
        if (!I2CBus.TryOpen(busPath, out var bus, out var openError))
        {
            return new NoOpDisplay(id, description, openError);
        }

        var channel = new DDCCIChannel(bus, _timings);
        // An "unsupported VCP code" reply still proves that the display speaks DDC/CI
        var status = channel.TryGetVCP((byte)VCPFeature.Brightness, out _, out var probeError);
        if (status == DDCStatus.Failed)
        {
            channel.Dispose();
            return new NoOpDisplay(id, description, probeError);
        }

        return new LinuxDisplay(channel, id, description, connector);
    }

    /// <summary>
    /// Finds the bus of a connector without a kernel-provided link (DisplayPort MST) by reading the EDID
    /// on each unused GPU bus. MST adapters (named "DPMST") are tried first, then DisplayPort AUX channels; the
    /// remaining buses (e.g. i915 gmbus pins used for HDMI) each cost a failed transfer.
    /// </summary>
    private static string? FindBusByEDID(DRMConnector connector, HashSet<string> usedBuses)
    {
        if (connector.EDID.Length < 128)
        {
            return null;
        }

        var expected = connector.EDID.AsSpan(0, 128);
        Span<byte> buffer = stackalloc byte[128];
        // A bus with a pending input switch write belongs to a display that is going away; reading
        // its EDID would block until the write times out
        var candidates = SysFs.GetGPUBuses(connector.CardDevicePath)
            .Where(b => !usedBuses.Contains(b.DevicePath) && !DDCCIChannel.IsWriteInBackground(b.DevicePath))
            .OrderBy(b => b.Name.StartsWith("DPMST", StringComparison.Ordinal) ? 0
                : b.Name.Contains("AUX", StringComparison.Ordinal) ? 1
                : 2);

        foreach (var (devicePath, _) in candidates)
        {
            if (!I2CBus.TryOpen(devicePath, out var bus, out _))
            {
                continue;
            }
            using (bus)
            {
                if (DDCCIChannel.TryReadEDID(bus, buffer, out _) && buffer.SequenceEqual(expected))
                {
                    return devicePath;
                }
            }
        }
        return null;
    }
}

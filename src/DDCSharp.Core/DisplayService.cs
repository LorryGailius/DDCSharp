using System.Diagnostics;
using DDCSharp.Core.Abstractions;

namespace DDCSharp.Core;

/// <summary>
/// Static display service that aggregates displays from registered providers.
/// </summary>
public static class DisplayService
{
    private static readonly List<IDisplayProvider> _providers = [];
    private static readonly object _lock = new();
    private static readonly TimeSpan InputSourcePollInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Registers a display provider. Thread-safe.
    /// </summary>
    public static void RegisterProvider(IDisplayProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
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
        IDisplayProvider[] snapshot;
        lock (_lock)
        {
            snapshot = [.. _providers];
        }

        var all = new List<IDisplay>();
        foreach (var provider in snapshot)
        {
            try
            {
                all.AddRange(provider.GetDisplays());
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
    /// DDC/CI are filtered out and disposed.
    /// </summary>
    public static IReadOnlyList<IDisplay> GetDisplays()
    {
        var displays = new List<IDisplay>();
        foreach (var display in GetAllDisplays())
        {
            if (display.SupportsVCP)
            {
                displays.Add(display);
            }
            else
            {
                display.Dispose();
            }
        }
        return displays;
    }

    /// <summary>
    /// Switches the input source and waits until the display reports it.
    /// </summary>
    /// <remarks>
    /// After the switch the handle of <paramref name="display"/> usually stops working (the operating system drops and
    /// re-adds the monitor, on Linux often on another I2C bus), so the confirmation enumerates displays again and finds
    /// the same monitor by <see cref="IDisplay.Id"/>. Measured on a Dell P2725DE this takes 2 to 3 seconds, or about
    /// 6 seconds on Linux when switching away from this computer over DisplayPort MST.
    /// </remarks>
    /// <param name="display">Display to switch.</param>
    /// <param name="target">Input source to switch to.</param>
    /// <param name="timeout">How long to wait for confirmation, 10 seconds if null.</param>
    /// <param name="cancellationToken">Cancels waiting; the switch command is already sent by then.</param>
    /// <returns>True once the display reports <paramref name="target"/>; false on timeout.</returns>
    public static Task<bool> SwitchInputSource(
        IDisplay display,
        InputSource target,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        display.SetInputSource(target);
        return WaitForInputSource(display.Id, target, timeout ?? TimeSpan.FromSeconds(10), cancellationToken);
    }

    /// <summary>
    /// Polls the display with the given <see cref="IDisplay.Id"/>, enumerating displays again on every attempt,
    /// until it reports <paramref name="target"/> as its input source.
    /// </summary>
    /// <returns>True once the display reports <paramref name="target"/>; false on timeout.</returns>
    public static async Task<bool> WaitForInputSource(
        string displayId,
        InputSource target,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        while (true)
        {
            using (var display = FindDisplay(displayId))
            {
                if (display != null && display.GetInputSource() == target)
                {
                    return true;
                }
            }

            if (Stopwatch.GetElapsedTime(start) >= timeout)
            {
                return false;
            }
            await Task.Delay(InputSourcePollInterval, cancellationToken);
        }
    }

    /// <summary>
    /// Enumerates displays again and returns the one with the given <see cref="IDisplay.Id"/>, or null if it is not attached.
    /// All other enumerated displays are disposed.
    /// </summary>
    public static IDisplay? FindDisplay(string id)
    {
        IDisplay? match = null;
        foreach (var display in GetAllDisplays())
        {
            if (match == null && display.SupportsVCP && display.Id == id)
            {
                match = display;
            }
            else
            {
                display.Dispose();
            }
        }
        return match;
    }
}

# DDCSharp
A .NET library for controlling displays via DDC/CI, including brightness, contrast, input source, and other display settings through a simple API

Lightweight C# (.NET 10) library for enumerating physical monitors and performing DDC/CI (MCCS VCP) VCP feature reads / writes (e.g. brightness, input source) with an extensible provider model.

## Projects

- `DDCSharp.Core` – Provider & transport abstractions plus capability / VCP feature models.
- `DDCSharp.Windows` – Windows implementation using `dxva2.dll` (Win32 APIs) to enumerate and control monitors.
- `DDCSharp.Linux` – Linux implementation that talks DDC/CI directly over `/dev/i2c-*` (no `ddcutil` needed).

## NuGet Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| DDCSharp.Core | [![NuGet](https://img.shields.io/nuget/v/DDCSharp.Core.svg)](https://www.nuget.org/packages/DDCSharp.Core) | Core abstractions + capability models |
| DDCSharp.Windows | [![NuGet](https://img.shields.io/nuget/v/DDCSharp.Windows.svg)](https://www.nuget.org/packages/DDCSharp.Windows) | Windows provider implementation |
| DDCSharp.Linux | [![NuGet](https://img.shields.io/nuget/v/DDCSharp.Linux.svg)](https://www.nuget.org/packages/DDCSharp.Linux) | Linux provider implementation |

Install via Package Manager:

```
PM> Install-Package DDCSharp.Core
PM> Install-Package DDCSharp.Windows
PM> Install-Package DDCSharp.Linux
```

Or with `dotnet` CLI:

```
dotnet add package DDCSharp.Core
dotnet add package DDCSharp.Windows
dotnet add package DDCSharp.Linux
```

## Key Concepts

- `IDisplay` – Abstraction of a physical monitor (read / write VCP features, refresh capabilities).
  - `Id` – Identifier built from the EDID (e.g. `DELF167-JF7R864`). It stays the same across enumerations, so use it to find a monitor again with `DisplayService.FindDisplay(id)` after its handle stops working.
  - `LastError` – Why the last operation failed (Win32 error, errno, or DDC/CI reply problem).
- `DDCDisplay` – Base class with the shared logic (input sources, brightness, capabilities). Providers only implement VCP reads, VCP writes and the capabilities request.
- `IDisplayProvider` – Produces `IDisplay` instances (platform / virtual). Multiple providers can be registered.
- `DisplayService` – Thread‑safe static registry to add / clear providers and obtain all displays.
- Capabilities Parsing – `CapabilityParser` parses the MCCS capability string into sections (`type`, `model`, `mccs_ver`, `vcp`) and a list of `Capability` entries (feature + supported values list if present). Capabilities are read from the display on first access because the request takes one to three seconds.

## Supported (subset) Features

Brightness, Contrast, Input Source, Sharpness, Color Gains, Color Presets, Power Mode, Speaker Volume, Audio Mute, Orientation, Frequencies, Panel/Tech info, Usage Time, etc. (See `VCPFeature` enum for details.)

## Quick Start

```csharp
using DDCSharp.Core;
using DDCSharp.Core.Abstractions;
using DDCSharp.Core.Capabilities;
using DDCSharp.Linux;
using DDCSharp.Windows;

// Register the platform provider once at startup
if (OperatingSystem.IsWindows())
{
    WindowsDisplayProvider.Register();
}
else if (OperatingSystem.IsLinux())
{
    LinuxDisplayProvider.Register();
}

// Enumerate displays
foreach (var display in DisplayService.GetDisplays())
{
    Console.WriteLine($"Id: {display.Id}  Description: {display.Description}  Model: {display.Model}  MCCS: {display.MCCSVersion}");

    // Read brightness
    if (display.TryGetVCPFeature(VCPFeature.Brightness, out var type, out var current, out var max))
    {
        Console.WriteLine($"Brightness: {current}/{max}");
    }

    // Set brightness (0..max range – caller responsibility)
    display.TrySetBrightness(50);

    // List supported input sources
    var sources = display.GetSupportedInputSources();
    Console.WriteLine("Inputs: " + string.Join(", ", sources));

    // Switch input and wait until the display reports it (handles go stale on a switch, so this re-enumerates)
    if (sources.Contains(InputSource.HDMI1))
    {
        var confirmed = await DisplayService.SwitchInputSource(display, InputSource.HDMI1);
    }
}
```

### Input switching

- `SetInputSource` only sends the command. When the display is showing this computer it drops the link as soon as it
  accepts the command, so the write reports an error although the switch happens; it is not retried.
- `DisplayService.SwitchInputSource` sends the command and confirms it by finding the display again by `Id`, which
  takes 2 to 3 seconds. On Linux, switching away from this computer over DisplayPort MST takes about 6 seconds to
  confirm because the kernel blocks for 4 seconds, although the display switches within a second.
  `DisplayService.WaitForInputSource` does only the confirmation.
- `GetLocalInputSource()` returns the input this computer is connected to, for displays that report it (e.g. Dell),
  so a "show this computer" action needs no configuration.

## Thread Safety

- Provider registration is synchronized (`DisplayService`).
- Windows displays synchronize native calls per display instance.
- Linux displays synchronize per I2C bus, across all display instances that use it.

## Extending (Custom Provider)

Implement `IDisplayProvider` and produce objects implementing `IDisplay`.

```csharp
public sealed class MyProvider : IDisplayProvider
{
    public IEnumerable<IDisplay> GetDisplays()
    {
        // Open native handles, wrap them in your IDisplay implementation
        yield break;
    }
}

DisplayService.RegisterProvider(new MyProvider());
```

Derive from `DDCDisplay` to get input source, brightness and capability handling for free; only `TryGetVCPFeature`, `TrySetVCPFeature` and `ReadCapabilitiesString` need implementing.

## Capability Refresh

Call `display.RefreshCapabilities()` to re-fetch the MCCS capability string (e.g., after OSD adjustments or hot‑plug events).

## Error Handling

- Provider enumeration exceptions are swallowed so one provider cannot block others.
- VCP read/write methods return `bool` indicating success; `LastError` describes the failure.
- A successful write only means the command was sent. DDC/CI has no acknowledgement, so read the value back to confirm it.

## Platform Notes

- Windows: uses `dxva2.dll` (`GetVCPFeatureAndVCPFeatureReply`, `SetVCPFeature`, capability string APIs) + `EnumDisplayMonitors`. The EDID for `Id` is read from the registry.
- Linux: finds connected monitors in `/sys/class/drm` and their I2C bus through the connector's `ddc` link. DisplayPort MST connectors (docks, USB-C monitors) have no link, so the bus is found by matching EDIDs. Laptop panels (eDP/LVDS/DSI) are reported without DDC/CI support. Timings can be tuned with `DDCTimings`.
- Some displays (e.g. Dell) put extra data in the high byte of the input source value; `GetInputSource()` only uses the low byte.
- Other platforms: supply a custom provider (e.g., USB HID, network bridge, mock/testing provider).

### Linux permissions

The `i2c-dev` kernel module must be loaded and the user needs read/write access to `/dev/i2c-*`. Distributions that package `ddcutil` usually set this up already. Otherwise:

```sh
# Load i2c-dev now and on every boot
sudo modprobe i2c-dev
echo i2c-dev | sudo tee /etc/modules-load.d/i2c-dev.conf

# Give the logged-in user access to I2C devices
echo 'KERNEL=="i2c-[0-9]*", SUBSYSTEM=="i2c-dev", TAG+="uaccess"' | sudo tee /etc/udev/rules.d/60-i2c-dev.rules
sudo udevadm control --reload && sudo udevadm trigger --subsystem-match=i2c-dev
```

## Disclaimer

Writing values to unsupported VCP features may fail silently. Some monitors expose incomplete or vendor-specific capability strings. Use at your own risk.

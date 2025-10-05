# DDCSharp
A .NET library for controlling displays via DDC/CI, including brightness, contrast, input source, and other display settings through a simple API

Lightweight C# (.NET 8) library for enumerating physical monitors and performing DDC/CI (MCCS VCP) VCP feature reads / writes (e.g. brightness, input source) with an extensible provider model.

## Projects

- `DDCSharp.Core` – Provider & transport abstractions plus capability / VCP feature models.
- `DDCSharp.Windows` – Windows implementation using `dxva2.dll` (Win32 APIs) to enumerate and control monitors.

## NuGet Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| DDCSharp.Core | [![NuGet](https://img.shields.io/nuget/v/DDCSharp.Core.svg)](https://www.nuget.org/packages/DDCSharp.Core) | Core abstractions + capability models |
| DDCSharp.Windows | [![NuGet](https://img.shields.io/nuget/v/DDCSharp.Windows.svg)](https://www.nuget.org/packages/DDCSharp.Windows) | Windows provider implementation |

Install via Package Manager:

```
PM> Install-Package DDCSharp.Core
PM> Install-Package DDCSharp.Windows
```

Or with `dotnet` CLI:

```
dotnet add package DDCSharp.Core
dotnet add package DDCSharp.Windows
```

## Key Concepts

- `IDisplay` – Abstraction of a physical monitor (read / write VCP features, refresh capabilities).
- `IDisplayProvider` – Produces `IDisplay` instances (platform / virtual). Multiple providers can be registered.
- `DisplayService` – Thread‑safe static registry to add / clear providers and obtain all displays.
- Capabilities Parsing – Windows provider fetches the MCCS capability string, extracts sections (`type`, `model`, `mccs_ver`, `vcp`) and builds a list of `Capability` entries (feature + supported values list if present).

## Supported (subset) Features

Brightness, Contrast, Input Source, Sharpness, Color Gains, Color Presets, Power Mode, Speaker Volume, Audio Mute, Orientation, Frequencies, Panel/Tech info, Usage Time, etc. (See `VCPFeature` enum for details.)

## Quick Start (Windows)

```csharp
using DDCSharp.Core;
using DDCSharp.Core.Abstractions;
using DDCSharp.Core.Capabilities;
using DDCSharp.Windows;

// Register the Windows provider once at startup
WindowsDisplayProvider.Register();

// Enumerate displays
foreach (var display in DisplayService.GetDisplays())
{
    Console.WriteLine($"Description: {display.Description}  Model: {display.Model}  MCCS: {display.MCCSVersion}");

    // Read brightness
    if (display.TryGetVcpFeature(VCPFeature.Brightness, out var type, out var current, out var max))
    {
        Console.WriteLine($"Brightness: {current}/{max}");
    }

    // Set brightness (0..max range – caller responsibility)
    display.TrySetBrightness(50);

    // List supported input sources
    var sources = display.GetSupportedInputSources();
    Console.WriteLine("Inputs: " + string.Join(", ", sources));

    // Change input if supported
    if (sources.Contains(InputSource.HDMI1))
    {
        display.TrySetInputSource(InputSource.HDMI1);
    }
}
```

## Thread Safety

- Provider registration is synchronized (`DisplayService`).
- Each `WindowsDisplay` synchronizes native calls per display instance.

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

`IDisplay` must expose metadata, capabilities, attempt VCP gets/sets, and dispose native resources.

## Capability Refresh

Call `display.RefreshCapabilities()` to re-fetch the MCCS capability string (e.g., after OSD adjustments or hot‑plug events).

## Error Handling

- Provider enumeration exceptions are swallowed so one provider cannot block others.
- VCP read/write methods return `bool` indicating success.

## Platform Notes

- Windows: uses `dxva2.dll` (`GetVCPFeatureAndVCPFeatureReply`, `SetVCPFeature`, capability string APIs) + `EnumDisplayMonitors`.
- Other platforms: supply a custom provider (e.g., DDC via I2C, USB HID, network bridge, mock/testing provider).

## Disclaimer

Writing values to unsupported VCP features may fail silently. Some monitors expose incomplete or vendor-specific capability strings. Use at your own risk.

using DDCSharp.Core.Capabilities;

namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Abstraction for a physical display. Some instances may not expose DDC/CI (VCP) control.
/// </summary>
public interface IDisplay : IDisposable
{
    // Properties

    /// <summary>
    /// Identifier built from the display's EDID (manufacturer, product code and serial number).
    /// It stays the same across enumerations, so it can be used to find the same physical display
    /// again after its native handle stops working (for example after an input switch).
    /// </summary>
    string Id { get; }
    /// <summary>Human-readable display name, taken from the EDID when available.</summary>
    string Description { get; }
    /// <summary>Indicates whether the underlying display exposes DDC/CI (MCCS) support. If false, calling any DDC operation will be unsuccessful.</summary>
    bool SupportsVCP { get; }
    /// <summary>Description of why the most recent operation failed, or null if it succeeded.</summary>
    string? LastError { get; }

    // Capabilities (loaded from the display on first access, which can take a few seconds)

    /// <summary>Reported display type section (if any) from capabilities string.</summary>
    string? Type { get; }
    /// <summary>Reported model section (if any) from capabilities string.</summary>
    string? Model { get; }
    /// <summary>MCCS version parsed from capabilities, or null.</summary>
    Version? MCCSVersion { get; }
    /// <summary>Parsed capability list (VCP feature codes and supported values).</summary>
    IReadOnlyCollection<Capability> Capabilities { get; }
    /// <summary>Raw MCCS capabilities string as reported by the display, or null if it could not be read.</summary>
    string? CapabilitiesString { get; }

    /// <summary>Re-query display MCCS capability string and update cached data.</summary>
    void RefreshCapabilities();

    // Generic VCP feature access

    /// <summary>
    /// Attempt to read a VCP feature using a raw byte code.
    /// </summary>
    /// <param name="code">Raw VCP feature code (0x00-0xFF) to query.</param>
    /// <param name="type">Returns the feature's native MCCS type classification.</param>
    /// <param name="currentValue">Returns the current (present) value reported by the display.</param>
    /// <param name="maximumValue">Returns the maximum value (for SetParameter features) or 0 if not applicable.</param>
    /// <returns>True if the feature was successfully read; otherwise false.</returns>
    bool TryGetVCPFeature(byte code, out VCPFeatureType type, out uint currentValue, out uint maximumValue);

    /// <summary>
    /// Attempt to read a VCP feature using a known enum value.
    /// </summary>
    /// <param name="code">Enumerated VCP feature identifier.</param>
    /// <param name="type">Returns the feature's native MCCS type classification.</param>
    /// <param name="currentValue">Returns the current (present) value reported by the display.</param>
    /// <param name="maximumValue">Returns the maximum value (for SetParameter features) or 0 if not applicable.</param>
    /// <returns>True if the feature was successfully read; otherwise false.</returns>
    bool TryGetVCPFeature(VCPFeature code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
        => TryGetVCPFeature((byte)code, out type, out currentValue, out maximumValue);

    /// <summary>
    /// Attempt to set a VCP feature value using a raw byte code (allows unknown / manufacturer specific codes).
    /// </summary>
    /// <param name="code">Raw VCP feature code (0x00-0xFF) to set.</param>
    /// <param name="value">Value to write. Must be within the valid range for the feature.</param>
    /// <returns>True if the command was sent; DDC/CI does not confirm that the display applied it.</returns>
    bool TrySetVCPFeature(byte code, uint value);

    /// <summary>
    /// Attempt to set a VCP feature value using a known enum value. Convenience wrapper over the raw byte overload.
    /// </summary>
    /// <param name="code">Enumerated VCP feature to set.</param>
    /// <param name="value">Value to write. Must be within the valid range for the feature.</param>
    /// <returns>True if the command was sent; DDC/CI does not confirm that the display applied it.</returns>
    bool TrySetVCPFeature(VCPFeature code, uint value) => TrySetVCPFeature((byte)code, value);

    // Specific common features

    /// <summary>Returns supported input sources determined from capability list.</summary>
    IReadOnlyCollection<InputSource> GetSupportedInputSources();

    /// <summary>
    /// Sends the command to change the current input source, without waiting for the display to switch.
    /// </summary>
    /// <remarks>
    /// No success indication is returned because the write result is not reliable: when the display is showing this
    /// computer, it drops the link as soon as it accepts the command, so the write usually reports an error although
    /// the switch happens. The handle also tends to stop working afterwards. Use
    /// <see cref="DisplayService.SwitchInputSource"/> to switch and confirm.
    /// </remarks>
    /// <param name="targetInput">Input source to switch to, normally one of <see cref="GetSupportedInputSources"/>.</param>
    void SetInputSource(InputSource targetInput);

    /// <summary>Read current input source, or <see cref="InputSource.Unknown"/> if it could not be read.</summary>
    InputSource GetInputSource();

    /// <summary>
    /// Returns the input this computer is connected to, or null if the display does not report it.
    /// </summary>
    /// <remarks>
    /// MCCS leaves the high byte of the input source value unused. Some displays (e.g. Dell) put the input the
    /// request arrived on there, which tells a computer which input to select to show itself.
    /// </remarks>
    InputSource? GetLocalInputSource();

    /// <summary>
    /// Attempt to set current brightness.
    /// </summary>
    /// <param name="brightness">
    /// Desired brightness value. Usually in the range 0-100 or 0-maximum as reported by the display's VCP Brightness feature.
    /// </param>
    /// <returns>True if brightness command was sent; otherwise false.</returns>
    bool TrySetBrightness(uint brightness);
}

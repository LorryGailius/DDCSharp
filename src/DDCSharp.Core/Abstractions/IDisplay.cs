using DDCSharp.Core.Capabilities;

namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Abstraction for a physical display. Some instances may not expose DDC/CI (VCP) control.
/// </summary>
public interface IDisplay : IDisposable
{
    // Properties

    /// <summary>Stable unique identifier for the display within the current session (e.g. logical device name + description).</summary>
    string Id { get; }
    /// <summary>Human-readable display description provided by the system.</summary>
    string Description { get; }
    /// <summary>Reported display type section (if any) from capabilities string.</summary>
    string? Type { get; }
    /// <summary>Reported model section (if any) from capabilities string.</summary>
    string? Model { get; }
    /// <summary>MCCS version parsed from capabilities, or null.</summary>
    Version? MCCSVersion { get; }
    /// <summary>Parsed capability list (VCP feature codes and supported values).</summary>
    IReadOnlyCollection<Capability> Capabilities { get; }
    /// <summary>Indicates whether the underlying display exposes DDC/CI (MCCS) support. If false, calling any DDC</summary>
    bool SupportsVCP { get; }

    // Capabilities management

    /// <summary>Re-query display MCCS capability string and update cached data.</summary>
    void RefreshCapabilities();

    // Generic VCP feature access

    /// <summary>
    /// Attempt to read a VCP feature using a raw byte code.
    /// </summary>
    /// <param name="code">Raw VCP feature code (0x00–0xFF) to query.</param>
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
    /// <param name="code">Raw VCP feature code (0x00–0xFF) to set.</param>
    /// <param name="value">Value to write. Must be within the valid range for the feature.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    bool TrySetVCPFeature(byte code, uint value);

    /// <summary>
    /// Attempt to set a VCP feature value using a known enum value. Convenience wrapper over the raw byte overload.
    /// </summary>
        /// <param name="code">Enumerated VCP feature to set.</param>
    /// <param name="value">Value to write. Must be within the valid range for the feature.</param>
    /// <returns>True if the operation succeeded; otherwise false.</returns>
    bool TrySetVCPFeature(VCPFeature code, uint value) => TrySetVCPFeature((byte)code, value);


    // Specific common features

    /// <summary>Returns supported input sources determined from capability list.</summary>
    IReadOnlyCollection<InputSource> GetSupportedInputSources();

    /// <summary>
    /// Attempt to change current input source.
    /// </summary>
    /// <param name="targetInput">Desired input source.</param>
    /// <param name="timeout">How long to wait for the source to apply before failing (null for implementation default).</param>
    /// <returns>True if the input source change was successful. Depending on the display the return may not properly reflect reality.</returns>
    bool TrySetInputSource(InputSource targetInput, TimeSpan? timeout = null);

    /// <summary>
    /// Attempt to change current input source asynchronously.
    /// </summary>
    /// <param name="targetInput">Desired input source.</param>
    /// <param name="timeout">How long to wait for the source to apply before failing (null for implementation default).</param>
    /// <param name="cancellationToken">Token used to cancel the pending operation.</param>
    /// <returns>True if the input source change was successful. Depending on the display the return may not properly reflect reality.</returns>
    Task<bool> TrySetInputSourceAsync(InputSource targetInput, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>Read current input source.</summary>
    InputSource GetInputSource();

    /// <summary>
    /// Attempt to set current brightness.
    /// </summary>
    /// <param name="brightness">
    /// Desired brightness value. Usually in the range 0–100 or 0–maximum as reported by the display's VCP Brightness feature.
    /// </param>
    /// <returns>True if brightness command was accepted; otherwise false.</returns>
    bool TrySetBrightness(uint brightness);
}

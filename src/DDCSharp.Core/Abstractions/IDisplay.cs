using DDCSharp.Core.Capabilities;

namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Abstraction for a physical display. Some instances may not expose DDC/CI (VCP) control.
/// </summary>
public interface IDisplay : IDisposable
{
    // Properties

    /// <summary>Human-readable monitor description provided by the system.</summary>
    string Description { get; }
    /// <summary>Reported monitor type section (if any) from capabilities string.</summary>
    string? Type { get; }
    /// <summary>Reported model section (if any) from capabilities string.</summary>
    string? Model { get; }
    /// <summary>MCCS version parsed from capabilities, or null.</summary>
    Version? MCCSVersion { get; }
    /// <summary>Parsed capability list (VCP feature codes and supported values).</summary>
    IReadOnlyCollection<Capability> Capabilities { get; }
    /// <summary>Indicates whether the underlying monitor exposes DDC/CI (MCCS) support.</summary>
    bool SupportsVCP { get; }

    // Capabilities management

    /// <summary>Re-query monitor MCCS capability string and update cached data.</summary>
    void RefreshCapabilities();

    // Generic VCP feature access

    /// <summary>
    /// Attempt to read a VCP feature using a raw byte code (allows unknown / manufacturer specific codes).
    /// </summary>
    bool TryGetVcpFeature(byte code, out VCPFeatureType type, out uint currentValue, out uint maximumValue);
    /// <summary>
    /// Attempt to read a VCP feature using a known enum value. Convenience wrapper over the raw byte overload.
    /// </summary>
    bool TryGetVcpFeature(VCPFeature code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
        => TryGetVcpFeature((byte)code, out type, out currentValue, out maximumValue);

    /// <summary>
    /// Attempt to set a VCP feature value using a raw byte code (allows unknown / manufacturer specific codes).
    /// </summary>
    bool TrySetVcpFeature(byte code, uint value);
    /// <summary>
    /// Attempt to set a VCP feature value using a known enum value. Convenience wrapper over the raw byte overload.
    /// </summary>
    bool TrySetVcpFeature(VCPFeature code, uint value) => TrySetVcpFeature((byte)code, value);


    // Specific common features

    /// <summary>Returns supported input sources determined from capability list.</summary>
    IReadOnlyCollection<InputSource> GetSupportedInputSources();
    /// <summary>Attempt to change current input source.</summary>
    bool TrySetInputSource(InputSource input);
    /// <summary>Attempt to set current brightness.</summary>
    bool TrySetBrightness(uint brightness);
}

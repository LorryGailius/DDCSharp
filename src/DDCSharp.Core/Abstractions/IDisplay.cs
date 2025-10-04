using DDCSharp.Core.Capabilities;

namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Abstraction for a physical display supporting DDC/CI VCP operations.
/// </summary>
public interface IDisplay : IDisposable
{
    // Properties

    /// <summary>Human readable monitor description provided by the system.</summary>
    string Description { get; }
    /// <summary>Reported monitor type section (if any) from capabilities string.</summary>
    string? Type { get; }
    /// <summary>Reported model section (if any) from capabilities string.</summary>
    string? Model { get; }
    /// <summary>MCCS version parsed from capabilities, or null.</summary>
    Version? MCCSVersion { get; }
    /// <summary>Parsed capability list (VCP feature codes and supported values).</summary>
    IReadOnlyCollection<Capability> Capabilities { get; }

    // Capabilities management

    /// <summary>Re-query monitor MCCS capability string and update cached data.</summary>
    void RefreshCapabilities();

    // Generic VCP feature access

    /// <summary>Attempt to read a VCP feature.</summary>
    bool TryGetVcpFeature(VCPFeature code, out VCPFeatureType type, out uint currentValue, out uint maximumValue);
    /// <summary>Attempt to set a VCP feature value.</summary>
    bool TrySetVcpFeature(VCPFeature code, uint value);


    // Specific common features

    /// <summary>Returns supported input sources determined from capability list.</summary>
    IReadOnlyCollection<InputSource> GetSupportedInputSources();
    /// <summary>Attempt to change current input source.</summary>
    bool TrySetInputSource(InputSource input);
    /// <summary>Attempt to set current brightness.</summary>
    bool TrySetBrightness(uint brightness);
}

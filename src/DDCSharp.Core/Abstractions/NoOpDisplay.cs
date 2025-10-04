using DDCSharp.Core.Capabilities;

namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Generic no-op display representing a monitor without VCP / DDC/CI support.
/// All control operations return false and capabilities are empty.
/// </summary>
public sealed class NoOpDisplay(
    string description,
    string? type = null,
    string? model = null,
    Version? mccsVersion = null)
    : IDisplay
{
    public string Description { get; } = description;
    public string? Type { get; } = type;
    public string? Model { get; } = model;
    public Version? MCCSVersion { get; } = mccsVersion;
    public IReadOnlyCollection<Capability> Capabilities => [];
    public bool SupportsVCP => false;

    public void RefreshCapabilities() { /* no-op */ }

    public bool TryGetVcpFeature(VCPFeature code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
    {
        type = default; currentValue = 0; maximumValue = 0; return false;
    }

    public bool TrySetVcpFeature(VCPFeature code, uint value) => false;
    public IReadOnlyCollection<InputSource> GetSupportedInputSources() => [];
    public bool TrySetInputSource(InputSource input) => false;
    public bool TrySetBrightness(uint brightness) => false;

    public void Dispose() { /* no-op */ }
}

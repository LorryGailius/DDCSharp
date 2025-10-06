using DDCSharp.Core.Capabilities;

namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Generic no-op display representing a monitor without VCP / DDC/CI support.
/// All control operations return false and capabilities are empty.
/// </summary>
public sealed class NoOpDisplay : IDisplay
{
    public string Description { get; }
    public string? Type { get; }
    public string? Model { get; }
    public Version? MCCSVersion { get; }
    public IReadOnlyCollection<Capability> Capabilities => [];
    public bool SupportsVCP => false;

    public NoOpDisplay(
        string description,
        string? type = null,
        string? model = null,
        Version? mccsVersion = null)
    {
        Description = description;
        Type = type;
        Model = model;
        MCCSVersion = mccsVersion;
    }

    public void RefreshCapabilities() { /* no-op */ }

    public bool TryGetVCPFeature(byte code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
    {
        type = default; currentValue = 0; maximumValue = 0; return false;
    }

    public bool TrySetVCPFeature(byte code, uint value) => false;

    public IReadOnlyCollection<InputSource> GetSupportedInputSources() => [];

    public void SetInputSource(InputSource targetInput) { /* no-op */ }

    public InputSource GetInputSource() => InputSource.Unknown;

    public bool TrySetBrightness(uint brightness) => false;

    public void Dispose() { /* no-op */ }
}

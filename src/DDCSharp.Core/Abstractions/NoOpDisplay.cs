using DDCSharp.Core.Capabilities;

namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Generic no-op display representing a monitor without VCP / DDC/CI support.
/// All control operations return false and capabilities are empty.
/// </summary>
public sealed class NoOpDisplay : IDisplay
{
    /// <inheritdoc />
    public string Id { get; }
    /// <inheritdoc />
    public string Description { get; }
    /// <inheritdoc />
    public bool SupportsVCP => false;
    /// <inheritdoc />
    public string? LastError { get; }
    /// <inheritdoc />
    public string? Type => null;
    /// <inheritdoc />
    public string? Model => null;
    /// <inheritdoc />
    public Version? MCCSVersion => null;
    /// <inheritdoc />
    public IReadOnlyCollection<Capability> Capabilities => [];
    /// <inheritdoc />
    public string? CapabilitiesString => null;

    /// <param name="id">Stable identifier, see <see cref="IDisplay.Id"/>.</param>
    /// <param name="description">Human-readable display name.</param>
    /// <param name="reason">Why the display cannot be controlled, reported through <see cref="LastError"/>.</param>
    public NoOpDisplay(string id, string description, string? reason = null)
    {
        Id = id;
        Description = description;
        LastError = reason ?? "Display does not support DDC/CI";
    }

    /// <inheritdoc />
    public void RefreshCapabilities() { /* no-op */ }

    /// <inheritdoc />
    public bool TryGetVCPFeature(byte code, out VCPFeatureType type, out uint currentValue, out uint maximumValue)
    {
        type = default; currentValue = 0; maximumValue = 0; return false;
    }

    /// <inheritdoc />
    public bool TrySetVCPFeature(byte code, uint value) => false;

    /// <inheritdoc />
    public IReadOnlyCollection<InputSource> GetSupportedInputSources() => [];

    /// <inheritdoc />
    public void SetInputSource(InputSource targetInput) { /* no-op */ }

    /// <inheritdoc />
    public InputSource GetInputSource() => InputSource.Unknown;

    /// <inheritdoc />
    public InputSource? GetLocalInputSource() => null;

    /// <inheritdoc />
    public bool TrySetBrightness(uint brightness) => false;

    /// <inheritdoc />
    public void Dispose() { /* no-op */ }

    /// <inheritdoc />
    public override string ToString() => $"{Description} [{Id}] no DDC/CI: {LastError}";
}

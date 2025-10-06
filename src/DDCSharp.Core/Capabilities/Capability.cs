namespace DDCSharp.Core.Capabilities;

/// <summary>
/// Represents a VCP feature capability entry and optionally its supported values list.
/// </summary>
public record Capability(
    byte Value,
    VCPFeature Feature,
    IReadOnlyList<byte> SupportedValues)
{
    /// <summary>Hexadecimal representation (two characters) of the feature code.</summary>
    public string Hex => Value.ToString("X2");
    /// <summary>Indicates whether the capability exposes an explicit set of supported values.</summary>
    public bool HasSupportedValues => SupportedValues.Count > 0;
    public override string ToString() =>
        HasSupportedValues
            ? $"{Hex} {Feature} [{string.Join(' ', SupportedValues.Select(v => v.ToString("X2")))}]"
            : $"{Hex} {Feature}";
}
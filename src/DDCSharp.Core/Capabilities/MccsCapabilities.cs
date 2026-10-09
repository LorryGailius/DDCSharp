namespace DDCSharp.Core.Capabilities;

/// <summary>
/// Parsed MCCS capabilities string.
/// </summary>
/// <param name="Raw">Capabilities string as reported by the display, or null if it could not be read.</param>
/// <param name="Type">Value of the <c>type</c> section (e.g. <c>LCD</c>).</param>
/// <param name="Model">Value of the <c>model</c> section.</param>
/// <param name="MccsVersion">Value of the <c>mccs_ver</c> section.</param>
/// <param name="Features">VCP features listed in the <c>vcp</c> section.</param>
/// <param name="Sections">All top-level sections keyed by lowercase name.</param>
public sealed record MccsCapabilities(
    string? Raw,
    string? Type,
    string? Model,
    Version? MccsVersion,
    IReadOnlyList<Capability> Features,
    IReadOnlyDictionary<string, string> Sections)
{
    /// <summary>Capabilities of a display whose capabilities string could not be read.</summary>
    public static MccsCapabilities Empty { get; } = new(null, null, null, null, [], new Dictionary<string, string>());
}

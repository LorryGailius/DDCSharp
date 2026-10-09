namespace DDCSharp.Core.Capabilities;

/// <summary>
/// VCP feature type classification. Values match the type byte of a DDC/CI Get VCP Feature reply.
/// </summary>
public enum VCPFeatureType : byte
{
    /// <summary>A setting that keeps its value (brightness, input source, ...).</summary>
    SetParameter = 0x00,
    /// <summary>A one-shot action (restore defaults, degauss, ...).</summary>
    Momentary = 0x01
}

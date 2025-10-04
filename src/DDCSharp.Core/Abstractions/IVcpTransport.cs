namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Abstraction for reading/writing MCCS (DDC/CI) VCP features.
/// </summary>
public interface IVcpTransport : IDisposable
{
    bool TryGet(byte code, out uint current, out uint maximum, out uint rawType);
    bool TrySet(byte code, uint value);
    string? TryGetCapabilities();
}

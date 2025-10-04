namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Public abstraction representing a native monitor handle used during monitor construction.
/// </summary>
public interface IMonitorHandle
{
    /// <summary>Raw native handle to the physical monitor (DXVA2).</summary>
    nint Handle { get; }
    /// <summary>System provided description string.</summary>
    string Description { get; }
}

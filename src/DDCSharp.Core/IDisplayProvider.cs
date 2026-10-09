using DDCSharp.Core.Abstractions;

namespace DDCSharp.Core;

/// <summary>
/// Contract for a display provider (platform specific or virtual).
/// </summary>
public interface IDisplayProvider
{
    /// <summary>
    /// Enumerates the displays currently attached. Every call opens new native handles, so callers
    /// must dispose the returned displays when done.
    /// </summary>
    IEnumerable<IDisplay> GetDisplays();
}

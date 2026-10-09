using DDCSharp.Core.Capabilities;

namespace DDCSharp.Core.Abstractions;

/// <summary>
/// Base class for displays controlled over DDC/CI. Platform providers implement the raw transport
/// (VCP reads, VCP writes and the capabilities request); this class implements everything built on top of it.
/// </summary>
public abstract class DdcDisplay : IDisplay
{
    private readonly object _capabilitiesLock = new();
    private MccsCapabilities? _capabilities;

    /// <summary>Creates a display with the given identity.</summary>
    /// <param name="id">Stable identifier, see <see cref="IDisplay.Id"/>.</param>
    /// <param name="description">Human-readable display name.</param>
    protected DdcDisplay(string id, string description)
    {
        Id = id;
        Description = description;
    }

    /// <inheritdoc />
    public string Id { get; }
    /// <inheritdoc />
    public string Description { get; }
    /// <inheritdoc />
    public bool SupportsVCP => true;
    /// <inheritdoc />
    public string? LastError { get; protected set; }

    /// <inheritdoc />
    public string? Type => GetCapabilities().Type;
    /// <inheritdoc />
    public string? Model => GetCapabilities().Model;
    /// <inheritdoc />
    public Version? MCCSVersion => GetCapabilities().MccsVersion;
    /// <inheritdoc />
    public IReadOnlyCollection<Capability> Capabilities => GetCapabilities().Features;
    /// <inheritdoc />
    public string? CapabilitiesString => GetCapabilities().Raw;

    /// <inheritdoc />
    /// <remarks>On failure the previously loaded capabilities are kept and <see cref="LastError"/> says why.</remarks>
    public void RefreshCapabilities()
    {
        var capabilities = ReadCapabilities();
        if (capabilities.Raw == null)
        {
            return;
        }

        lock (_capabilitiesLock)
        {
            _capabilities = capabilities;
        }
    }

    private MccsCapabilities GetCapabilities()
    {
        lock (_capabilitiesLock)
        {
            if (_capabilities != null)
            {
                return _capabilities;
            }

            var capabilities = ReadCapabilities();
            // Failures are not cached so a display that was briefly unreachable is retried on next access
            if (capabilities.Raw != null)
            {
                _capabilities = capabilities;
            }
            return capabilities;
        }
    }

    private MccsCapabilities ReadCapabilities()
    {
        var raw = ReadCapabilitiesString();
        return raw == null ? MccsCapabilities.Empty : CapabilityParser.Parse(raw);
    }

    /// <summary>
    /// Reads the MCCS capabilities string from the display.
    /// </summary>
    /// <returns>The raw capabilities string, or null (with <see cref="LastError"/> set) if it could not be read.</returns>
    protected abstract string? ReadCapabilitiesString();

    /// <inheritdoc />
    public abstract bool TryGetVCPFeature(byte code, out VCPFeatureType type, out uint currentValue, out uint maximumValue);

    /// <inheritdoc />
    public abstract bool TrySetVCPFeature(byte code, uint value);

    /// <inheritdoc />
    public IReadOnlyCollection<InputSource> GetSupportedInputSources()
    {
        var inputCapability = Capabilities.FirstOrDefault(c => c.Feature == VCPFeature.InputSource);
        if (inputCapability == null)
        {
            return [];
        }

        return inputCapability.SupportedValues
            .Select(v => (InputSource)v)
            .ToList();
    }

    /// <inheritdoc />
    public void SetInputSource(InputSource targetInput) =>
        TrySetVCPFeature((byte)VCPFeature.InputSource, (byte)targetInput);

    /// <inheritdoc />
    public InputSource GetInputSource()
    {
        if (!TryGetVCPFeature((byte)VCPFeature.InputSource, out _, out var currentValue, out _))
        {
            return InputSource.Unknown;
        }

        // Some displays (e.g. Dell) repeat the input code in the high byte, only the low byte is meaningful
        return (InputSource)(byte)currentValue;
    }

    /// <inheritdoc />
    public InputSource? GetLocalInputSource()
    {
        if (!TryGetVCPFeature((byte)VCPFeature.InputSource, out _, out var currentValue, out _))
        {
            return null;
        }

        var local = (byte)(currentValue >> 8);
        return local != 0 ? (InputSource)local : null;
    }

    /// <inheritdoc />
    public bool TrySetBrightness(uint brightness) => TrySetVCPFeature((byte)VCPFeature.Brightness, brightness);

    /// <summary>Clears <see cref="LastError"/> and returns true.</summary>
    protected bool Succeed()
    {
        LastError = null;
        return true;
    }

    /// <summary>Sets <see cref="LastError"/> and returns false.</summary>
    protected bool Fail(string error)
    {
        LastError = error;
        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases native resources held by the display.</summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}

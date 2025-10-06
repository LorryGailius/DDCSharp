using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DDCSharp.Core.Capabilities;

namespace DDCSharp.Windows;

internal sealed class WindowsDisplayInfo
{
    public string? Type { get; init; }
    public string? Model { get; init; }
    public Version? MccsVersion { get; init; }
    public IReadOnlyCollection<Capability> Capabilities { get; init; } = [];
    public string Description { get; init; } = string.Empty;
    public bool SupportsVCP { get; init; }

    public static WindowsDisplayInfo Create(WindowsDisplayHandle handle)
    {
        var raw = GetCapabilities(handle).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            // No capabilities string -> no DDC/CI
            return new WindowsDisplayInfo { Description = handle.Description, SupportsVCP = false };
        }
        if (raw.StartsWith('(') && raw.EndsWith(')'))
        {
            raw = raw[1..^1].Trim();
        }
        var regex = new Regex(@"([a-z_]{3,})\(([\s\S]*?)\)(?=(?:[a-z_]{3,}\()|$)");
        var sections = regex.Matches(raw)
            .Where(m => m.Groups.Count == 3)
            .Select(m => (Key: m.Groups[1].Value, m.Groups[2].Value))
            .ToDictionary();
        sections.TryGetValue("type", out var type);
        sections.TryGetValue("model", out var model);
        sections.TryGetValue("mccs_ver", out var mccsVer);
        sections.TryGetValue("vcp", out var rawVcp);
        return new WindowsDisplayInfo
        {
            Description = handle.Description,
            Type = type,
            Model = model,
            MccsVersion = mccsVer != null && Version.TryParse(mccsVer, out var ver) ? ver : null,
            Capabilities = rawVcp != null ? GetFeatures(rawVcp) : [],
            SupportsVCP = true
        };
    }

    private static string GetCapabilities(WindowsDisplayHandle handle)
    {
        if (!WinAPI.GetCapabilitiesStringLength(handle.Handle, out var len) || len == 0)
            return string.Empty;
        var sb = new StringBuilder((int)len);
        if (!WinAPI.CapabilitiesRequestAndCapabilitiesReply(handle.Handle, sb, len))
            return string.Empty;
        return sb.ToString();
    }

    private static IReadOnlyCollection<Capability> GetFeatures(string rawVcp)
    {
        var regex = new Regex(@"([0-9A-F]{2})(?:\(([0-9A-F\s]+)\))?");
        return regex.Matches(rawVcp)
            .Select(m =>
            {
                var hex = m.Groups[1].Value;
                var code = byte.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var feature = Enum.IsDefined(typeof(VCPFeature), (VCPFeature)code)
                    ? (VCPFeature)code
                    : VCPFeature.Unknown;
                IReadOnlyList<byte> supportedValues = [];
                if (m.Groups.Count > 2 && m.Groups[2].Success)
                {
                    var valuesText = m.Groups[2].Value;
                    if (!string.IsNullOrWhiteSpace(valuesText))
                    {
                        // Some manufacturers use VCP values larger than one byte
                        supportedValues = valuesText
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Where(token => token.Length <= 2)
                            .Select(token => 
                                byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)
                                ? (byte?)b
                                : null)
                            .Where(b => b.HasValue)
                            .Select(b => b!.Value)
                            .Distinct()
                            .ToArray();
                    }
                }
                return new Capability(code, feature, supportedValues);
            })
            .ToList();
    }
}

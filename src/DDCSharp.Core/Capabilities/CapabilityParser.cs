using System.Globalization;

namespace DDCSharp.Core.Capabilities;

/// <summary>
/// Parser for MCCS capabilities strings such as
/// <c>(prot(monitor)type(LCD)model(P2725DE)vcp(10 12 60(0F 11 1B))mccs_ver(2.1))</c>.
/// </summary>
public static class CapabilityParser
{
    /// <summary>Parses a raw capabilities string. Malformed parts are skipped instead of throwing.</summary>
    public static MccsCapabilities Parse(string raw)
    {
        var sections = ParseSections(raw);
        sections.TryGetValue("type", out var type);
        sections.TryGetValue("model", out var model);
        sections.TryGetValue("mccs_ver", out var mccsVersion);
        sections.TryGetValue("vcp", out var vcp);

        return new MccsCapabilities(
            raw,
            type,
            model,
            mccsVersion != null && Version.TryParse(mccsVersion, out var version) ? version : null,
            vcp != null ? ParseVcp(vcp) : [],
            sections);
    }

    private static Dictionary<string, string> ParseSections(string raw)
    {
        var text = raw.AsSpan().Trim().TrimEnd('\0').Trim();
        // The whole string is normally wrapped in one pair of parentheses, which may be unbalanced on truncated replies
        if (text.Length > 0 && text[0] == '(')
        {
            text = text[1..];
            if (text.Length > 0 && text[^1] == ')')
            {
                text = text[..^1];
            }
        }

        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var position = 0;
        while (position < text.Length)
        {
            var open = text[position..].IndexOf('(');
            if (open < 0)
            {
                break;
            }

            var key = text.Slice(position, open).Trim().ToString().ToLowerInvariant();
            var bodyStart = position + open + 1;
            var bodyEnd = FindClosingParenthesis(text, bodyStart);
            if (key.Length > 0)
            {
                sections.TryAdd(key, text[bodyStart..bodyEnd].Trim().ToString());
            }
            position = bodyEnd + 1;
        }
        return sections;
    }

    /// <summary>Returns the index of the parenthesis closing the group opened just before <paramref name="start"/>, or the text length if it is never closed.</summary>
    private static int FindClosingParenthesis(ReadOnlySpan<char> text, int start)
    {
        var depth = 1;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '(')
            {
                depth++;
            }
            else if (text[i] == ')' && --depth == 0)
            {
                return i;
            }
        }
        return text.Length;
    }

    private static List<Capability> ParseVcp(string vcp)
    {
        var text = vcp.AsSpan();
        var capabilities = new List<Capability>();
        var position = 0;
        while (position < text.Length)
        {
            if (!char.IsAsciiHexDigit(text[position]))
            {
                position++;
                continue;
            }

            var runStart = position;
            while (position < text.Length && char.IsAsciiHexDigit(text[position]))
            {
                position++;
            }
            // Feature codes are always one byte; some displays omit the spaces between them ("101214")
            var codes = ParseHexPairs(text[runStart..position]);

            var next = position;
            while (next < text.Length && char.IsWhiteSpace(text[next]))
            {
                next++;
            }

            IReadOnlyList<byte> supportedValues = [];
            if (next < text.Length && text[next] == '(')
            {
                var valuesEnd = FindClosingParenthesis(text, next + 1);
                supportedValues = ParseValueList(text[(next + 1)..valuesEnd]);
                position = valuesEnd + 1;
            }

            for (var i = 0; i < codes.Count; i++)
            {
                var isLast = i == codes.Count - 1;
                capabilities.Add(CreateCapability(codes[i], isLast ? supportedValues : []));
            }
        }
        return capabilities;
    }

    private static IReadOnlyList<byte> ParseValueList(ReadOnlySpan<char> text)
    {
        var tokens = new List<string>();
        var position = 0;
        while (position < text.Length)
        {
            if (text[position] == '(')
            {
                // Nested groups (MCCS 3 sub-values) are not modelled
                position = FindClosingParenthesis(text, position + 1) + 1;
                continue;
            }
            if (!char.IsAsciiHexDigit(text[position]))
            {
                position++;
                continue;
            }

            var tokenStart = position;
            while (position < text.Length && char.IsAsciiHexDigit(text[position]))
            {
                position++;
            }
            tokens.Add(text[tokenStart..position].ToString());
        }

        // A single long token is a list written without spaces; otherwise long tokens are
        // vendor values wider than a byte, which the byte-based model cannot represent
        if (tokens.Count == 1 && tokens[0].Length > 2)
        {
            return ParseHexPairs(tokens[0]).Distinct().ToArray();
        }

        return tokens
            .Where(token => token.Length <= 2)
            .Select(token => byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .Distinct()
            .ToArray();
    }

    private static List<byte> ParseHexPairs(ReadOnlySpan<char> hex)
    {
        var values = new List<byte>(hex.Length / 2 + 1);
        if (hex.Length % 2 == 1)
        {
            values.Add(byte.Parse(hex[..1], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            hex = hex[1..];
        }
        for (var i = 0; i < hex.Length; i += 2)
        {
            values.Add(byte.Parse(hex.Slice(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }
        return values;
    }

    private static Capability CreateCapability(byte code, IReadOnlyList<byte> supportedValues)
    {
        var feature = Enum.IsDefined((VCPFeature)code) ? (VCPFeature)code : VCPFeature.Unknown;
        return new Capability(code, feature, supportedValues);
    }
}

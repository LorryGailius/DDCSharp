using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DDCSharp.Core;

/// <summary>
/// Identity fields decoded from the base block of a display's EDID.
/// </summary>
/// <param name="ManufacturerId">Three-letter PNP manufacturer ID (e.g. <c>DEL</c>).</param>
/// <param name="ProductCode">Manufacturer product code.</param>
/// <param name="SerialNumber">Binary serial number, 0 if not set.</param>
/// <param name="SerialText">Serial number descriptor text, if present.</param>
/// <param name="Name">Monitor name descriptor text (e.g. <c>DELL P2725DE</c>), if present.</param>
public sealed record EdidInfo(
    string ManufacturerId,
    ushort ProductCode,
    uint SerialNumber,
    string? SerialText,
    string? Name)
{
    private static ReadOnlySpan<byte> Header => [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];

    /// <summary>Decodes the first 128 bytes of an EDID.</summary>
    public static bool TryParse(ReadOnlySpan<byte> data, [NotNullWhen(true)] out EdidInfo? edid)
    {
        edid = null;
        if (data.Length < 128 || !data[..8].SequenceEqual(Header))
        {
            return false;
        }

        // Manufacturer ID: three 5-bit letters packed big-endian, 1 = 'A'
        var packed = BinaryPrimitives.ReadUInt16BigEndian(data[8..]);
        var manufacturer = new string([
            (char)('A' - 1 + ((packed >> 10) & 0x1F)),
            (char)('A' - 1 + ((packed >> 5) & 0x1F)),
            (char)('A' - 1 + (packed & 0x1F))]);

        string? name = null;
        string? serialText = null;
        for (var offset = 54; offset <= 108; offset += 18)
        {
            var descriptor = data.Slice(offset, 18);
            if (descriptor[0] != 0 || descriptor[1] != 0 || descriptor[2] != 0)
            {
                continue;
            }
            switch (descriptor[3])
            {
                case 0xFC:
                    name ??= ReadDescriptorText(descriptor);
                    break;
                case 0xFF:
                    serialText ??= ReadDescriptorText(descriptor);
                    break;
            }
        }

        edid = new EdidInfo(
            manufacturer,
            BinaryPrimitives.ReadUInt16LittleEndian(data[10..]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[12..]),
            serialText,
            name);
        return true;
    }

    /// <summary>
    /// Builds a display identifier such as <c>DELF167-JF7R864</c>. Displays without any serial number
    /// get <paramref name="location"/> appended instead so identical models can still be told apart.
    /// </summary>
    public string ToDisplayId(string? location = null)
    {
        var model = $"{ManufacturerId}{ProductCode:X4}";
        if (!string.IsNullOrEmpty(SerialText))
        {
            return $"{model}-{SerialText}";
        }
        if (SerialNumber != 0)
        {
            return $"{model}-{SerialNumber:X8}";
        }
        return string.IsNullOrEmpty(location) ? model : $"{model}@{location}";
    }

    private static string? ReadDescriptorText(ReadOnlySpan<byte> descriptor)
    {
        var text = descriptor[5..];
        var end = text.IndexOf((byte)0x0A);
        if (end >= 0)
        {
            text = text[..end];
        }
        var value = Encoding.ASCII.GetString(text).Trim();
        return value.Length > 0 ? value : null;
    }
}

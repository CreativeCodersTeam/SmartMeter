namespace CreativeCoders.SmartMessageLanguage.Framing;

/// <summary>
/// CRC-16/X-25 implementation as used by the SML transport v1 protocol.
/// </summary>
/// <remarks>
/// Parameters: polynomial <c>0x1021</c>, initial value <c>0xFFFF</c>, reflected input
/// and output, final XOR <c>0xFFFF</c>. After computation SML stores the CRC little-endian
/// in the wire, so reading the two final bytes as a little-endian <see cref="ushort"/>
/// yields the computed value directly.
/// </remarks>
internal static class Crc16X25
{
    private static readonly ushort[] _table = BuildTable();

    /// <summary>Computes the CRC-16/X-25 over the given buffer.</summary>
    /// <param name="data">Bytes to compute the CRC over.</param>
    /// <returns>The 16-bit CRC value.</returns>
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;

        foreach (var b in data)
        {
            crc = (ushort)((crc >> 8) ^ _table[(crc ^ b) & 0xFF]);
        }

        return (ushort)(crc ^ 0xFFFF);
    }

    private static ushort[] BuildTable()
    {
        // Reflected polynomial for CRC-16/X-25 (0x1021 reversed = 0x8408).
        const ushort reflectedPoly = 0x8408;

        var table = new ushort[256];

        for (var i = 0; i < 256; i++)
        {
            var value = (ushort)i;

            for (var bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0
                    ? (ushort)((value >> 1) ^ reflectedPoly)
                    : (ushort)(value >> 1);
            }

            table[i] = value;
        }

        return table;
    }
}

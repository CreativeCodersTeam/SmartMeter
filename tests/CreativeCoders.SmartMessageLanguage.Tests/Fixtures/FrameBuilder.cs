using CreativeCoders.SmartMessageLanguage.Framing;

namespace CreativeCoders.SmartMessageLanguage.Tests.Fixtures;

/// <summary>
/// Wraps a raw (de-escaped) payload into a complete SML transport v1 frame with
/// correct escape-doubling, padding and CRC-16/X-25.
/// </summary>
internal static class FrameBuilder
{
    public static byte[] BuildFrame(byte[] payload)
    {
        // Escape doubling: any four consecutive 0x1B bytes in payload become eight on the wire.
        var escaped = EscapePayload(payload);

        // 4-byte alignment padding is computed on the escaped body length (libSML convention).
        var paddingBytes = (4 - (escaped.Count % 4)) % 4;

        var body = new List<byte>();
        body.AddRange([0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01, 0x01, 0x01]);
        body.AddRange(escaped);

        for (var i = 0; i < paddingBytes; i++)
        {
            body.Add(0x00);
        }

        body.AddRange([0x1B, 0x1B, 0x1B, 0x1B, 0x1A, (byte)paddingBytes]);

        var preCrc = body.ToArray();
        var crc = Crc16X25.Compute(preCrc);
        body.Add((byte)(crc & 0xFF));
        body.Add((byte)((crc >> 8) & 0xFF));

        return body.ToArray();
    }

    private static List<byte> EscapePayload(byte[] payload)
    {
        var output = new List<byte>(payload.Length);
        var i = 0;

        while (i < payload.Length)
        {
            if (i + 4 <= payload.Length
                && payload[i] == 0x1B && payload[i + 1] == 0x1B
                && payload[i + 2] == 0x1B && payload[i + 3] == 0x1B)
            {
                output.AddRange([0x1B, 0x1B, 0x1B, 0x1B, 0x1B, 0x1B, 0x1B, 0x1B]);
                i += 4;
            }
            else
            {
                output.Add(payload[i]);
                i++;
            }
        }

        return output;
    }
}

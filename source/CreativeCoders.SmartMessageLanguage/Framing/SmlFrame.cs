namespace CreativeCoders.SmartMessageLanguage.Framing;

/// <summary>
/// A complete SML transport v1 frame extracted from a byte stream.
/// </summary>
/// <param name="MessageBytes">
/// Raw bytes of the frame as received, including start escape (<c>1B1B1B1B 01010101</c>),
/// original escape doubling of <c>0x1B</c> runs in the body, padding, and end escape
/// (<c>1B1B1B1B 1A &lt;pad&gt; &lt;crc-lo&gt; &lt;crc-hi&gt;</c>).
/// </param>
/// <param name="PayloadBytes">
/// Message body between start and end escape, already de-escaped (any doubled <c>0x1B</c>
/// runs collapsed to single runs) and with the trailing padding zeros stripped.
/// Ready to feed into <c>SmlTlvReader</c>.
/// </param>
/// <param name="IsCrcValid">
/// <c>true</c> when the CRC-16/X-25 computed over the frame (up to and including the
/// pad byte) matches the two CRC bytes at the end of the frame.
/// </param>
/// <param name="PaddingBytes">Number of <c>0x00</c> padding bytes at the end of the body (0-3).</param>
public sealed record SmlFrame(
    byte[] MessageBytes,
    byte[] PayloadBytes,
    bool IsCrcValid,
    int PaddingBytes);

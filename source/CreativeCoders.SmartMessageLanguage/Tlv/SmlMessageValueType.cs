namespace CreativeCoders.SmartMessageLanguage.Tlv;

/// <summary>
/// Primitive type classification for an <see cref="SmlTlvElement"/>.
/// </summary>
/// <remarks>
/// Enum values are stable identifiers for API consumers; they intentionally differ
/// from the SML TLV type nibble because the end-of-message marker (<c>0x00</c>) shares
/// the same wire nibble (<c>0x0</c>) as an octet string. The reader discriminates on
/// the full first byte when deciding which enum value to assign.
/// </remarks>
public enum SmlMessageValueType
{
    /// <summary>End-of-message marker (single <c>0x00</c> byte).</summary>
    EndOfMessage = 0,

    /// <summary>Octet string (type nibble <c>0x0</c>).</summary>
    OctetString = 1,

    /// <summary>Boolean (type nibble <c>0x4</c>).</summary>
    Boolean = 2,

    /// <summary>Two's complement signed integer (type nibble <c>0x5</c>).</summary>
    Integer = 3,

    /// <summary>Unsigned integer (type nibble <c>0x6</c>).</summary>
    Unsigned = 4,

    /// <summary>List of nested TLV elements (type nibble <c>0x7</c>).</summary>
    List = 5
}

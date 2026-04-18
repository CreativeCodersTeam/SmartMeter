namespace CreativeCoders.SmartMessageLanguage.Tlv;

/// <summary>
/// Descriptor of a single TLV element read by <see cref="SmlTlvReader"/>.
/// </summary>
/// <remarks>
/// Primitive elements carry their raw payload bytes in <see cref="Raw"/>. Lists expose
/// their declared entry count in <see cref="ListLength"/>; use the enclosing
/// <see cref="SmlTlvReader"/> to descend into them with subsequent <c>Read</c> calls.
/// </remarks>
public readonly ref struct SmlTlvElement
{
    internal SmlTlvElement(SmlValueType type, int listLength, ReadOnlySpan<byte> raw)
    {
        Type = type;
        ListLength = listLength;
        Raw = raw;
    }

    /// <summary>The TLV primitive or structural type.</summary>
    public SmlValueType Type { get; }

    /// <summary>Declared number of entries when <see cref="Type"/> is <see cref="SmlValueType.List"/>.</summary>
    public int ListLength { get; }

    /// <summary>
    /// Raw payload bytes for primitive elements (empty for <see cref="SmlValueType.List"/>
    /// and <see cref="SmlValueType.EndOfMessage"/>).
    /// </summary>
    public ReadOnlySpan<byte> Raw { get; }

    /// <summary><c>true</c> if this element is the end-of-message marker (<c>0x00</c>).</summary>
    public bool IsEndOfMessage => Type == SmlValueType.EndOfMessage;

    /// <summary>Parses <see cref="Raw"/> as a big-endian unsigned integer (1-8 bytes).</summary>
    public ulong GetUInt64()
    {
        ulong value = 0;

        foreach (var b in Raw)
        {
            value = (value << 8) | b;
        }

        return value;
    }

    /// <summary>
    /// Parses <see cref="Raw"/> as a big-endian two's-complement signed integer (1-8 bytes).
    /// </summary>
    public long GetInt64()
    {
        if (Raw.IsEmpty)
        {
            return 0;
        }

        // Sign-extend the most significant byte to a full 64-bit register.
        long value = (sbyte)Raw[0];

        for (var i = 1; i < Raw.Length; i++)
        {
            value = (value << 8) | Raw[i];
        }

        return value;
    }

    /// <summary>Returns the boolean value. Any non-zero byte is treated as <c>true</c>.</summary>
    public bool GetBool()
    {
        foreach (var b in Raw)
        {
            if (b != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the octet string bytes as a new array.</summary>
    public byte[] GetOctetString() => Raw.ToArray();
}

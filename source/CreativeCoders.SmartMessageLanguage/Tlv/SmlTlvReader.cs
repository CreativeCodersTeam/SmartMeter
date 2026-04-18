namespace CreativeCoders.SmartMessageLanguage.Tlv;

/// <summary>
/// Low-level, allocation-free forward walker over SML TLV data.
/// </summary>
/// <remarks>
/// The reader decodes the TL header (type nibble + length nibble) and any chained
/// length extension bytes (<c>0x8x</c> continuation nibbles). For primitive types the
/// <see cref="Current"/> element exposes the raw payload slice. For lists,
/// <see cref="SmlTlvElement.ListLength"/> gives the entry count and subsequent
/// <see cref="Read"/> calls descend depth-first into the children.
/// </remarks>
public ref struct SmlTlvReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;
    private SmlTlvElement _current;

    /// <summary>Creates a new reader positioned at the start of <paramref name="data"/>.</summary>
    public SmlTlvReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
        _current = default;
    }

    /// <summary>The element most recently produced by <see cref="Read"/>.</summary>
    public SmlTlvElement Current => _current;

    /// <summary>Current byte offset inside the underlying data span.</summary>
    public int Position => _position;

    /// <summary><c>true</c> if the reader has consumed all bytes.</summary>
    public readonly bool EndOfData => _position >= _data.Length;

    /// <summary>Advances to the next element. Returns <c>false</c> when no more data is available.</summary>
    public bool Read()
    {
        if (_position >= _data.Length)
        {
            return false;
        }

        var first = _data[_position];

        // End-of-message marker.
        if (first == 0x00)
        {
            _position++;
            _current = new SmlTlvElement(SmlMessageValueType.EndOfMessage, 0, ReadOnlySpan<byte>.Empty);

            return true;
        }

        var typeNibble = (first >> 4) & 0x07;
        // High bit of the type nibble set signals a length extension byte follows.
        var hasMoreLengthBytes = (first & 0x80) != 0;
        var length = first & 0x0F;
        var lengthBytesConsumed = 1;

        while (hasMoreLengthBytes)
        {
            if (_position + lengthBytesConsumed >= _data.Length)
            {
                throw new InvalidOperationException("Truncated TLV length field");
            }

            var next = _data[_position + lengthBytesConsumed];
            hasMoreLengthBytes = (next & 0x80) != 0;
            length = (length << 4) | (next & 0x0F);
            lengthBytesConsumed++;
        }

        var headerLength = lengthBytesConsumed;

        switch (typeNibble)
        {
            case 0x7:
            {
                // For lists, the 'length' encodes the number of child elements, not a byte count.
                _position += headerLength;
                _current = new SmlTlvElement(SmlMessageValueType.List, length, ReadOnlySpan<byte>.Empty);

                return true;
            }
            case 0x0:
            case 0x4:
            case 0x5:
            case 0x6:
            {
                var resolvedType = typeNibble switch
                {
                    0x0 => SmlMessageValueType.OctetString,
                    0x4 => SmlMessageValueType.Boolean,
                    0x5 => SmlMessageValueType.Integer,
                    _ => SmlMessageValueType.Unsigned
                };

                // Declared length includes the header byte(s); payload length is length - header.
                var payloadLength = length - headerLength;

                if (payloadLength < 0 || _position + headerLength + payloadLength > _data.Length)
                {
                    throw new InvalidOperationException("Truncated TLV element");
                }

                var payload = _data.Slice(_position + headerLength, payloadLength);
                _position += headerLength + payloadLength;
                _current = new SmlTlvElement(resolvedType, 0, payload);

                return true;
            }
            default:
                throw new InvalidOperationException($"Unknown SML TLV type nibble 0x{typeNibble:X}");
        }
    }

    /// <summary>
    /// Skips the element most recently returned by <see cref="Read"/>. For lists this recursively
    /// consumes all children.
    /// </summary>
    public void SkipCurrent()
    {
        if (_current.Type != SmlMessageValueType.List)
        {
            // Primitives have already been fully consumed by Read().
            return;
        }

        var remaining = _current.ListLength;

        while (remaining > 0)
        {
            if (!Read())
            {
                throw new InvalidOperationException("Unexpected end of data while skipping list");
            }

            SkipCurrent();
            remaining--;
        }
    }
}

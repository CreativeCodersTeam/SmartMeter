using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace CreativeCoders.SmartMessageLanguage.Tests.Fixtures;

/// <summary>
/// Minimal builder producing SML TLV encoded byte sequences for tests.
/// Only supports lengths up to 15 (single-byte TL header), which is sufficient
/// for the structures exercised by the parser tests.
/// </summary>
internal sealed class TlvBuilder
{
    private readonly List<byte> _bytes = [];

    [SuppressMessage("csharpsquid", "S2437", Justification = "This is a test fixture, so we can ignore the warning")]
    public TlvBuilder List(int count)
    {
        if (count > 0x0F)
        {
            // Simple multi-byte length for lists: 0x7? 0x0? with continuation bit.
            _bytes.Add((byte)(0xF0 | ((count >> 4) & 0x0F)));
            _bytes.Add((byte)(0x00 | (count & 0x0F)));

            return this;
        }

        _bytes.Add((byte)(0x70 | (count & 0x0F)));

        return this;
    }

    public TlvBuilder OctetString(byte[] data)
    {
        AddPrimitive(0x00, data);

        return this;
    }

    public TlvBuilder Bool(bool value)
    {
        AddPrimitive(0x40, [value ? (byte)0x01 : (byte)0x00]);

        return this;
    }

    public TlvBuilder Int8(sbyte value)
    {
        AddPrimitive(0x50, [(byte)value]);

        return this;
    }

    public TlvBuilder Int32(int value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf, value);
        AddPrimitive(0x50, buf.ToArray());

        return this;
    }

    public TlvBuilder UInt8(byte value)
    {
        AddPrimitive(0x60, [value]);

        return this;
    }

    public TlvBuilder UInt32(uint value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buf, value);
        AddPrimitive(0x60, buf.ToArray());

        return this;
    }

    public TlvBuilder UInt64(ulong value)
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buf, value);
        AddPrimitive(0x60, buf.ToArray());

        return this;
    }

    public TlvBuilder EndOfMessage()
    {
        _bytes.Add(0x00);

        return this;
    }

    public TlvBuilder Null()
    {
        // OPTIONAL absent value is encoded as a zero-length octet string (0x01).
        _bytes.Add(0x01);

        return this;
    }

    public TlvBuilder Append(TlvBuilder other)
    {
        _bytes.AddRange(other._bytes);

        return this;
    }

    public byte[] ToArray() => _bytes.ToArray();

    private void AddPrimitive(byte typeNibble, byte[] payload)
    {
        // Declared length in the TL header is header bytes + payload bytes.
        var total = payload.Length + 1;

        if (total > 0x0F)
        {
            // Two-byte length header with continuation bit set on the first byte.
            _bytes.Add((byte)(0x80 | typeNibble | (((total + 1) >> 4) & 0x0F)));
            _bytes.Add((byte)((total + 1) & 0x0F));
        }
        else
        {
            _bytes.Add((byte)(typeNibble | (total & 0x0F)));
        }

        _bytes.AddRange(payload);
    }
}

using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Tlv;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests.Tlv;

public class SmlTlvReaderTests
{
    [Fact]
    public void Read_OctetString_ReturnsPayload()
    {
        // 0x04 header = OctetString of declared length 4 → 3 payload bytes.
        var data = new byte[] { 0x04, 0xDE, 0xAD, 0xBE };

        var reader = new SmlTlvReader(data);
        reader.Read().Should().BeTrue();

        reader.Current.Type.Should().Be(SmlValueType.OctetString);
        reader.Current.Raw.ToArray().Should().Equal(0xDE, 0xAD, 0xBE);
    }

    [Fact]
    public void Read_Unsigned_ReturnsBigEndianValue()
    {
        // 0x63 header = Unsigned of declared length 3 → 2 payload bytes (big-endian).
        var data = new byte[] { 0x63, 0x01, 0x02 };

        var reader = new SmlTlvReader(data);
        reader.Read().Should().BeTrue();

        reader.Current.Type.Should().Be(SmlValueType.Unsigned);
        reader.Current.GetUInt64().Should().Be(0x0102UL);
    }

    [Fact]
    public void Read_SignedInteger_SignExtends()
    {
        // Int8 with value -1 (0xFF).
        var data = new byte[] { 0x52, 0xFF };

        var reader = new SmlTlvReader(data);
        reader.Read().Should().BeTrue();

        reader.Current.Type.Should().Be(SmlValueType.Integer);
        reader.Current.GetInt64().Should().Be(-1);
    }

    [Fact]
    public void Read_Bool_ReturnsTrue()
    {
        var data = new byte[] { 0x42, 0x01 };

        var reader = new SmlTlvReader(data);
        reader.Read().Should().BeTrue();

        reader.Current.Type.Should().Be(SmlValueType.Boolean);
        reader.Current.GetBool().Should().BeTrue();
    }

    [Fact]
    public void Read_List_ReportsEntryCountAndIteratesChildren()
    {
        // List(2) of [Unsigned8(1), Unsigned8(2)].
        var data = new byte[] { 0x72, 0x62, 0x01, 0x62, 0x02 };

        var reader = new SmlTlvReader(data);
        reader.Read().Should().BeTrue();
        reader.Current.Type.Should().Be(SmlValueType.List);
        reader.Current.ListLength.Should().Be(2);

        reader.Read().Should().BeTrue();
        reader.Current.GetUInt64().Should().Be(1UL);

        reader.Read().Should().BeTrue();
        reader.Current.GetUInt64().Should().Be(2UL);

        reader.Read().Should().BeFalse();
    }

    [Fact]
    public void Read_EndOfMessage_IsRecognised()
    {
        var data = new byte[] { 0x00 };

        var reader = new SmlTlvReader(data);
        reader.Read().Should().BeTrue();

        reader.Current.IsEndOfMessage.Should().BeTrue();
    }

    [Fact]
    public void Read_MultiByteLength_ParsesCorrectly()
    {
        // 0x81 declares continuation, low nibble 1; next byte 0x02 gives total = (1<<4)|2 = 18.
        // So OctetString with 18-byte total header+payload → 16 payload bytes.
        var payload = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        var data = new byte[] { 0x81, 0x02 }.Concat(payload).ToArray();

        var reader = new SmlTlvReader(data);
        reader.Read().Should().BeTrue();

        reader.Current.Type.Should().Be(SmlValueType.OctetString);
        reader.Current.Raw.Length.Should().Be(16);
    }

    [Fact]
    public void SkipCurrent_OnList_ConsumesAllChildrenRecursively()
    {
        // Nested: List(1) containing List(2) containing UInt8(1), UInt8(2), then a trailing UInt8(9).
        var data = new byte[]
        {
            0x71, 0x72, 0x62, 0x01, 0x62, 0x02,
            0x62, 0x09
        };

        var reader = new SmlTlvReader(data);
        reader.Read().Should().BeTrue();
        reader.Current.Type.Should().Be(SmlValueType.List);
        reader.SkipCurrent();

        reader.Read().Should().BeTrue();
        reader.Current.GetUInt64().Should().Be(9UL);
    }
}

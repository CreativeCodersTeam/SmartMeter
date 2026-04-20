using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Tlv;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests.Tlv;

public class SmlTlvElementTests
{
    [Fact]
    public void GetUInt64_WithEmptyPayload_ReturnsZero()
    {
        // 0x61 = Unsigned declared length 1 → 0 payload bytes.
        var reader = new SmlTlvReader(new byte[] { 0x61 });
        reader.Read();

        reader.Current.GetUInt64().Should().Be(0UL);
    }

    [Theory]
    [InlineData(new byte[] { 0x62, 0x2A }, 0x2AUL)]
    [InlineData(new byte[] { 0x63, 0x01, 0x02 }, 0x0102UL)]
    [InlineData(new byte[] { 0x65, 0x12, 0x34, 0x56, 0x78 }, 0x12345678UL)]
    public void GetUInt64_WithVariousSizes_ReturnsBigEndianValue(byte[] data, ulong expected)
    {
        var reader = new SmlTlvReader(data);
        reader.Read();

        reader.Current.GetUInt64().Should().Be(expected);
    }

    [Fact]
    public void GetInt64_WithEmptyPayload_ReturnsZero()
    {
        // 0x51 = Integer declared length 1 → 0 payload bytes.
        var reader = new SmlTlvReader(new byte[] { 0x51 });
        reader.Read();

        reader.Current.GetInt64().Should().Be(0);
    }

    [Theory]
    [InlineData(new byte[] { 0x52, 0x7F }, 127L)]
    [InlineData(new byte[] { 0x52, 0x80 }, -128L)]
    [InlineData(new byte[] { 0x53, 0xFF, 0x00 }, -256L)]
    [InlineData(new byte[] { 0x55, 0xFF, 0xFF, 0xFF, 0xFF }, -1L)]
    public void GetInt64_SignExtendsCorrectly(byte[] data, long expected)
    {
        var reader = new SmlTlvReader(data);
        reader.Read();

        reader.Current.GetInt64().Should().Be(expected);
    }

    [Theory]
    [InlineData(new byte[] { 0x42, 0x01 }, true)]
    [InlineData(new byte[] { 0x42, 0xFF }, true)]
    [InlineData(new byte[] { 0x42, 0x00 }, false)]
    [InlineData(new byte[] { 0x43, 0x00, 0x00 }, false)]
    [InlineData(new byte[] { 0x43, 0x00, 0x01 }, true)]
    public void GetBool_ReturnsTrueIfAnyNonZeroByte(byte[] data, bool expected)
    {
        var reader = new SmlTlvReader(data);
        reader.Read();

        reader.Current.GetBool().Should().Be(expected);
    }

    [Fact]
    public void GetOctetString_ReturnsCopyOfPayload()
    {
        var data = new byte[] { 0x04, 0xDE, 0xAD, 0xBE };
        var reader = new SmlTlvReader(data);
        reader.Read();

        var copy = reader.Current.GetOctetString();

        copy.Should().Equal(0xDE, 0xAD, 0xBE);
        copy.Should().NotBeSameAs(data);
    }

    [Fact]
    public void IsEndOfMessage_OnlyTrueForEndMarker()
    {
        var reader = new SmlTlvReader(new byte[] { 0x00, 0x62, 0x01 });

        reader.Read();
        reader.Current.IsEndOfMessage.Should().BeTrue();

        reader.Read();
        reader.Current.IsEndOfMessage.Should().BeFalse();
    }

    [Fact]
    public void ListLength_IsZeroForPrimitives()
    {
        var reader = new SmlTlvReader(new byte[] { 0x62, 0x01 });
        reader.Read();

        reader.Current.ListLength.Should().Be(0);
    }
}

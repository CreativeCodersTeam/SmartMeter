using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Framing;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests.Framing;

public class Crc16X25Tests
{
    [Fact]
    public void Compute_WithStandardCheckString_ReturnsKnownCheckValue()
    {
        // Standard CRC-16/X-25 check value for ASCII "123456789" is 0x906E.
        var bytes = "123456789"u8.ToArray();

        var actual = Crc16X25.Compute(bytes);

        actual.Should().Be((ushort)0x906E);
    }

    [Fact]
    public void Compute_WithEmptyBuffer_ReturnsZero()
    {
        var actual = Crc16X25.Compute([]);

        // Empty input: initial 0xFFFF XOR final 0xFFFF = 0x0000.
        actual.Should().Be((ushort)0x0000);
    }
}

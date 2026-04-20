using System.Diagnostics.CodeAnalysis;
using System.Text;
using AwesomeAssertions;
using CreativeCoders.SmartMeter.Core.Unlock;
using Xunit;

namespace CreativeCoders.SmartMeter.Core.Tests.Unlock;

[SuppressMessage("ReSharper", "UseUtf8StringLiteral")]
public class ObisCodeScannerTests
{
    [Theory]
    [InlineData("1-0:1.8.0*255", new byte[] { 1, 0, 1, 8, 0, 255 })]
    [InlineData("1-0:16.7.0*255", new byte[] { 1, 0, 16, 7, 0, 255 })]
    [InlineData("0-0:0.0.0*0", new byte[] { 0, 0, 0, 0, 0, 0 })]
    public void ParseObis_WithValidString_ReturnsSixBytes(string input, byte[] expected)
    {
        // Act
        var result = ObisCodeScanner.ParseObis(input);

        // Assert
        result.Should().Equal(expected);
    }

    [Theory]
    [InlineData("1.0:1.8.0*255")]
    [InlineData("1-0-1.8.0*255")]
    [InlineData("1-0:1.8.0")]
    [InlineData("1-0:1.8*255")]
    [InlineData("1-0:1.8.0.0*255")]
    public void ParseObis_WithMalformedString_ThrowsFormatException(string input)
    {
        // Act
        var act = () => ObisCodeScanner.ParseObis(input);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseObis_WithEmptyString_Throws(string input)
    {
        // Act
        var act = () => ObisCodeScanner.ParseObis(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseObis_WithByteOverflow_ThrowsOverflowException()
    {
        // Act
        var act = () => ObisCodeScanner.ParseObis("300-0:1.8.0*255");

        // Assert
        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void FindMatches_WhenPatternIsContained_ReturnsMatchingCode()
    {
        // Arrange
        var payload = new byte[] { 0xAA, 0xBB, 1, 0, 1, 8, 0, 255, 0xCC };
        var expected = new[] { "1-0:1.8.0*255", "1-0:2.8.0*255" };

        // Act
        var matches = ObisCodeScanner.FindMatches(payload, expected).ToArray();

        // Assert
        matches.Should().ContainSingle().Which.Should().Be("1-0:1.8.0*255");
    }

    [Fact]
    public void FindMatches_WhenMultiplePatternsPresent_ReturnsAll()
    {
        // Arrange
        var payload = new byte[] { 1, 0, 1, 8, 0, 255, 0, 1, 0, 2, 8, 0, 255 };
        var expected = new[] { "1-0:1.8.0*255", "1-0:2.8.0*255" };

        // Act
        var matches = ObisCodeScanner.FindMatches(payload, expected).ToArray();

        // Assert
        matches.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void FindMatches_WhenPayloadShorterThanPattern_ReturnsEmpty()
    {
        // Arrange
        var payload = new byte[] { 1, 0, 1 };
        var expected = new[] { "1-0:1.8.0*255" };

        // Act
        var matches = ObisCodeScanner.FindMatches(payload, expected).ToArray();

        // Assert
        matches.Should().BeEmpty();
    }

    [Fact]
    public void FindMatches_WithEmptyExpectedList_ReturnsEmpty()
    {
        // Arrange
        var payload = Encoding.ASCII.GetBytes("whatever");

        // Act
        var matches = ObisCodeScanner.FindMatches(payload, []).ToArray();

        // Assert
        matches.Should().BeEmpty();
    }

    [Fact]
    public void FindMatches_WithEmptyPayload_ReturnsEmpty()
    {
        // Arrange
        var expected = new[] { "1-0:1.8.0*255" };

        // Act
        var matches = ObisCodeScanner.FindMatches([], expected).ToArray();

        // Assert
        matches.Should().BeEmpty();
    }

    [Fact]
    public void FindMatches_WithNullPayload_Throws()
    {
        // Act
        var act = () => ObisCodeScanner.FindMatches(null!, []).ToArray();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}

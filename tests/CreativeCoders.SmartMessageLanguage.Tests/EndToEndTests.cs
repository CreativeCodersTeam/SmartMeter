using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Parsing;
using CreativeCoders.SmartMessageLanguage.Tests.Fixtures;
using CreativeCoders.SmartMessageLanguage.Units;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests;

public class EndToEndTests
{
    [Fact]
    public void DetectorToParser_RoundTripsToExpectedObisValues()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        SmlFrame? detected = null;
        detector.MessageReceived += (_, e) => detected = e.Frame;

        // Feed in two chunks to exercise the streaming path.
        detector.Append(frameBytes.AsSpan(0, 10));
        detector.Append(frameBytes.AsSpan(10));

        detected.Should().NotBeNull();
        detected!.IsCrcValid.Should().BeTrue();

        var parser = new SmlParser(NullLogger<SmlParser>.Instance);
        var result = parser.Parse(detected);

        result.Values.Should().HaveCount(2);
        result.Values.Select(v => v.ObisCode).Should()
            .BeEquivalentTo(["1-0:1.8.0*255", "1-0:16.7.0*255"]);
        result.Values.Single(v => v.Unit == SmlUnit.WattHour).Value.Should().Be(12345.6m);
        result.Values.Single(v => v.Unit == SmlUnit.Watt).Value.Should().Be(567m);
    }
}

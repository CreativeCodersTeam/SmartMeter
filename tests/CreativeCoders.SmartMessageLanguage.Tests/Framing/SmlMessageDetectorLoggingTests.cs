using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Tests.Fixtures;
using CreativeCoders.SmartMessageLanguage.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests.Framing;

public class SmlMessageDetectorLoggingTests
{
    [Fact]
    public void Append_ValidFrame_LogsAnchorAndInformationFrameDetected()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);
        var logger = LoggerCallAssertions.CreateEnabledLogger<SmlMessageDetector>();

        using var detector = new SmlMessageDetector(logger);
        detector.Append(frameBytes);

        // Anchor (Debug, 1002) + FrameDetected (Information, 1003) both appear once.
        LoggerCallAssertions.CountCalls(logger, LogLevel.Debug, 1002).Should().Be(1);
        LoggerCallAssertions.CountCalls(logger, LogLevel.Information, 1003).Should().Be(1);
        LoggerCallAssertions.CountCalls(logger, LogLevel.Warning).Should().Be(0);
    }

    [Fact]
    public void Append_CorruptCrc_LogsInvalidCrcWarning()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);
        // Flip a CRC byte to force a CRC mismatch.
        frameBytes[^1] ^= 0xFF;

        var logger = LoggerCallAssertions.CreateEnabledLogger<SmlMessageDetector>();

        using var detector = new SmlMessageDetector(logger);
        detector.Append(frameBytes);

        LoggerCallAssertions.CountCalls(logger, LogLevel.Warning, 1010).Should().Be(1);
    }

    [Fact]
    public void Append_BufferOverflow_LogsBufferOverflowWarning()
    {
        // Send a start escape followed by 70k bytes of garbage so no end is found.
        var logger = LoggerCallAssertions.CreateEnabledLogger<SmlMessageDetector>();

        using var detector = new SmlMessageDetector(logger);
        detector.Append([0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01, 0x01, 0x01]);
        detector.Append(new byte[70 * 1024]);

        LoggerCallAssertions.CountCalls(logger, LogLevel.Warning, 1012).Should().Be(1);
    }
}

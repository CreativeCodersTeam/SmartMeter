using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests.Framing;

public class SmlMessageDetectorTests
{
    [Fact]
    public void Append_FullFrameInOneCall_RaisesOneEventWithValidCrc()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append(frameBytes);

        received.Should().HaveCount(1);
        received[0].IsCrcValid.Should().BeTrue();
        received[0].MessageBytes.Should().Equal(frameBytes);
    }

    [Fact]
    public void Append_FrameByteByByte_StillProducesSingleFrame()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        foreach (var b in frameBytes)
        {
            detector.Append([b]);
        }

        received.Should().HaveCount(1);
        received[0].IsCrcValid.Should().BeTrue();
    }

    [Fact]
    public void Append_GarbageBeforeStartEscape_DiscardsJunkAndEmitsFrame()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append([0xAA, 0xBB, 0xCC, 0xDD]);
        detector.Append(frameBytes);

        received.Should().HaveCount(1);
        received[0].IsCrcValid.Should().BeTrue();
    }

    [Fact]
    public void Append_TwoConsecutiveFrames_EmitsBothInOrder()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);
        var combined = frameBytes.Concat(frameBytes).ToArray();

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append(combined);

        received.Should().HaveCount(2);
        received.Should().AllSatisfy(f => f.IsCrcValid.Should().BeTrue());
    }

    [Fact]
    public void Append_EscapedPayload_DeEscapesAndValidatesCrc()
    {
        // Payload that contains a literal run of four 0x1B bytes.
        var payload = new byte[] { 0x1B, 0x1B, 0x1B, 0x1B, 0x09, 0x08 };
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append(frameBytes);

        received.Should().HaveCount(1);
        received[0].IsCrcValid.Should().BeTrue();
        // De-escaped payload should start with the original 4x0x1B run.
        received[0].PayloadBytes.AsSpan(0, 6).ToArray().Should()
            .Equal(0x1B, 0x1B, 0x1B, 0x1B, 0x09, 0x08);
    }

    [Fact]
    public void Append_TruncatedFrame_DoesNotEmitUntilCompletion()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append(frameBytes.AsSpan(0, frameBytes.Length - 4));
        received.Should().BeEmpty();

        detector.Append(frameBytes.AsSpan(frameBytes.Length - 4));
        received.Should().HaveCount(1);
    }

    [Fact]
    public void Append_CorruptCrc_EmitsFrameWithIsCrcValidFalse()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);
        frameBytes[^1] ^= 0xFF;

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append(frameBytes);

        received.Should().HaveCount(1);
        received[0].IsCrcValid.Should().BeFalse();
    }

    [Fact]
    public void Messages_Observable_EmitsSameFrameAsEvent()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var viaObservable = new List<SmlFrame>();
        using var sub = detector.Messages.Subscribe(viaObservable.Add);

        detector.Append(frameBytes);

        viaObservable.Should().HaveCount(1);
        viaObservable[0].MessageBytes.Should().Equal(frameBytes);
    }
}

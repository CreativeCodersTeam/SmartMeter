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

    [Fact]
    public void Append_EmptyData_IsNoOp()
    {
        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append(ReadOnlySpan<byte>.Empty);

        received.Should().BeEmpty();
    }

    [Fact]
    public void Append_StartEscapeSplitAcrossCalls_StillDetectsFrame()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        // Split inside the 8-byte start escape sequence so the detector has to keep
        // a small tail buffer across Append calls.
        detector.Append(frameBytes.AsSpan(0, 3));
        detector.Append(frameBytes.AsSpan(3));

        received.Should().HaveCount(1);
        received[0].IsCrcValid.Should().BeTrue();
    }

    [Fact]
    public void Reset_AfterPartialStartEscape_DropsBufferedData()
    {
        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append([0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01]);
        detector.Reset();

        // After reset a new full frame must still be detected normally.
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        detector.Append(FrameBuilder.BuildFrame(payload));

        received.Should().HaveCount(1);
    }

    [Fact]
    public void Append_MalformedEndEscape_RecoversAndFindsNextFrame()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var good = FrameBuilder.BuildFrame(payload);

        // Build a malformed frame: start escape + 4 body bytes + stray 4x 0x1B followed by a
        // non-0x1A, non-0x1B byte that is not part of a valid end escape. The detector must
        // resync and then parse the following good frame.
        var malformed = new byte[]
        {
            0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01, 0x01, 0x01,
            0xAA, 0xBB, 0xCC, 0xDD,
            0x1B, 0x1B, 0x1B, 0x1B, 0x42, 0x00, 0x00, 0x00
        };

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append(malformed);
        detector.Append(good);

        received.Should().HaveCount(1);
        received[0].IsCrcValid.Should().BeTrue();
    }

    [Fact]
    public void Dispose_CompletesObservable()
    {
        var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var completed = false;
        using var sub = detector.Messages.Subscribe(_ => { }, () => completed = true);

        ((IDisposable)detector).Dispose();

        completed.Should().BeTrue();
    }

    [Fact]
    public void Append_FrameContainingOddPadding_DeEscapesCorrectly()
    {
        // Payload whose length produces 2 bytes of padding (libSML aligns to 4).
        var payload = new byte[] { 0x42, 0x42 };
        var frameBytes = FrameBuilder.BuildFrame(payload);

        using var detector = new SmlMessageDetector(NullLogger<SmlMessageDetector>.Instance);
        var received = new List<SmlFrame>();
        detector.MessageReceived += (_, e) => received.Add(e.Frame);

        detector.Append(frameBytes);

        received.Should().ContainSingle();
        received[0].IsCrcValid.Should().BeTrue();
        received[0].PayloadBytes.Should().Equal(0x42, 0x42);
        received[0].PaddingBytes.Should().Be(2);
    }
}

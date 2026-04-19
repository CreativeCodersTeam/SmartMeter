using System.Text;
using AwesomeAssertions;
using CreativeCoders.SmartMeter.Core.Tests.Fixtures;
using CreativeCoders.SmartMeter.Core.Unlock;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CreativeCoders.SmartMeter.Core.Tests.Unlock;

public class SmartMeterUnlockerTests
{
    private static (SmartMeterUnlocker Sut, FakeReactiveSerialPort Port) CreateSut(
        SmartMeterOptions? options = null)
    {
        var port = new FakeReactiveSerialPort();
        var factory = A.Fake<IReactiveSerialPortFactory>();
        A.CallTo(() => factory.Create(A<string>._)).Returns(port);

        var sut = new SmartMeterUnlocker(
            NullLogger<SmartMeterUnlocker>.Instance,
            factory,
            Options.Create(options ?? new SmartMeterOptions()));

        return (sut, port);
    }

    private static SmartMeterUnlockOptions FastOptions(SmartMeterUnlockOptions? baseOptions = null) =>
        (baseOptions ?? new SmartMeterUnlockOptions()) with
        {
            InitialDelay = TimeSpan.Zero,
            VerificationTimeout = TimeSpan.FromMilliseconds(500),
            DigitDelay = TimeSpan.Zero
        };

    [Fact]
    public async Task UnlockAsync_WhenVerifySkipped_WritesPinAndReturnsSkipped()
    {
        // Arrange
        var (sut, port) = CreateSut();
        var options = FastOptions() with { Verify = false, LineEnding = "\r\n" };

        // Act
        var result = await sut.UnlockAsync("00000000", options);

        // Assert
        result.Success.Should().BeTrue();
        result.Outcome.Should().Be(SmartMeterUnlockOutcome.VerificationSkipped);
        port.OpenCount.Should().Be(1);
        port.Writes.Should().ContainSingle()
            .Which.Should().Equal(Encoding.ASCII.GetBytes("00000000\r\n"));
    }

    [Fact]
    public async Task UnlockAsync_WhenExpectedObisCodeObserved_ReturnsPinAccepted()
    {
        // Arrange
        var (sut, port) = CreateSut();
        var options = FastOptions() with
        {
            ExpectedObisCodes = ["1-0:1.8.0*255"]
        };

        // Act
        var unlockTask = sut.UnlockAsync("12345678", options);
        // Give the subscription a moment to register, then push the matching OBIS bytes
        await Task.Delay(50);
        port.PushBytes([0xFF, 1, 0, 1, 8, 0, 255, 0xAA]);

        var result = await unlockTask;

        // Assert
        result.Success.Should().BeTrue();
        result.Outcome.Should().Be(SmartMeterUnlockOutcome.PinAccepted);
        result.DetectedObisCodes.Should().ContainSingle().Which.Should().Be("1-0:1.8.0*255");
    }

    [Fact]
    public async Task UnlockAsync_WhenNoEvidenceWithinTimeout_ReturnsVerificationTimeout()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var options = FastOptions() with
        {
            VerificationTimeout = TimeSpan.FromMilliseconds(100),
            ExpectedObisCodes = ["1-0:1.8.0*255"]
        };

        // Act
        var result = await sut.UnlockAsync("00000000", options);

        // Assert
        result.Success.Should().BeFalse();
        result.Outcome.Should().Be(SmartMeterUnlockOutcome.VerificationTimeout);
        result.DetectedObisCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task UnlockAsync_WithIskraStrategyAndAckByte_ReturnsPinAccepted()
    {
        // Arrange
        var (sut, port) = CreateSut();
        var options = FastOptions() with { Strategy = SmartMeterPinStrategy.IskraAsciiBlock };

        // Act
        var unlockTask = sut.UnlockAsync("00000000", options);
        await Task.Delay(50);
        port.PushBytes([0x06]);

        var result = await unlockTask;

        // Assert
        result.Success.Should().BeTrue();
        result.Outcome.Should().Be(SmartMeterUnlockOutcome.PinAccepted);
    }

    [Fact]
    public async Task UnlockAsync_WhenCancelledBeforeSend_ReturnsCancelled()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var options = FastOptions() with { InitialDelay = TimeSpan.FromSeconds(5) };
        using var cts = new CancellationTokenSource();

        // Act
        var task = sut.UnlockAsync("00000000", options, cts.Token);
        cts.Cancel();

        var result = await task;

        // Assert
        result.Success.Should().BeFalse();
        result.Outcome.Should().Be(SmartMeterUnlockOutcome.Cancelled);
    }

    [Fact]
    public async Task UnlockAsync_WhenWriteThrows_ReturnsWriteFailed()
    {
        // Arrange
        var (sut, port) = CreateSut();
        port.WriteBehavior = _ => new InvalidOperationException("boom");
        var options = FastOptions();

        // Act
        var result = await sut.UnlockAsync("00000000", options);

        // Assert
        result.Success.Should().BeFalse();
        result.Outcome.Should().Be(SmartMeterUnlockOutcome.WriteFailed);
        result.Message.Should().Contain("boom");
    }

    [Fact]
    public async Task UnlockAsync_WithDigitByDigitStrategy_WritesEachDigitSeparately()
    {
        // Arrange
        var (sut, port) = CreateSut();
        var options = FastOptions() with
        {
            Strategy = SmartMeterPinStrategy.EasymeterDigitByDigit,
            Verify = false
        };

        // Act
        var result = await sut.UnlockAsync("1234", options);

        // Assert
        result.Outcome.Should().Be(SmartMeterUnlockOutcome.VerificationSkipped);
        port.Writes.Should().HaveCount(4);
        port.Writes.Select(w => w[0]).Should().Equal((byte)'1', (byte)'2', (byte)'3', (byte)'4');
    }

    [Fact]
    public async Task UnlockAsync_WhenPortAlreadyOpen_DoesNotOpenAgain()
    {
        // Arrange
        var (sut, port) = CreateSut();
        port.Open();
        var initialOpenCount = port.OpenCount;

        // Act
        await sut.UnlockAsync("00000000", FastOptions() with { Verify = false });

        // Assert
        port.OpenCount.Should().Be(initialOpenCount);
    }

    [Fact]
    public async Task UnlockAsync_WithWhitespacePin_ThrowsArgumentException()
    {
        // Arrange
        var (sut, _) = CreateSut();

        // Act
        var act = () => sut.UnlockAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UnlockAsync_WithLineEnding_AppendsCorrectly()
    {
        // Arrange
        var (sut, port) = CreateSut();
        var options = FastOptions() with { Verify = false, LineEnding = "\n" };

        // Act
        await sut.UnlockAsync("ABCD", options);

        // Assert
        port.Writes.Single().Should().Equal(Encoding.ASCII.GetBytes("ABCD\n"));
    }

    [Fact]
    public void Dispose_DisposesUnderlyingPort()
    {
        // Arrange
        var (sut, port) = CreateSut();

        // Act
        sut.Dispose();

        // Assert
        port.DisposeCount.Should().Be(1);
    }
}

using System.Reactive.Subjects;
using AwesomeAssertions;
using CreativeCoders.SmartMeter.Core.SmlData;
using CreativeCoders.SmartMeter.Core.Tests.Fixtures;
using CreativeCoders.SmartMeter.DataProcessing;
using FakeItEasy;
using FakeItEasy.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CreativeCoders.SmartMeter.Core.Tests.SmlData;

public class SmartMeterDataProducerTests
{
    private static (SmartMeterDataProducer Sut, FakeReactiveSerialPort Port,
        ISmartMeterReactiveDataPipeline Pipeline) CreateSut()
    {
        var port = new FakeReactiveSerialPort();
        var factory = A.Fake<IReactiveSerialPortFactory>();
        A.CallTo(() => factory.Create(A<string>._)).Returns(port);

        var pipeline = A.Fake<ISmartMeterReactiveDataPipeline>();
        // Make the pipeline observable behave like an empty subject — no values emitted.
        var subject = new Subject<SmartMeterValue>();
        A.CallTo(() => pipeline.Subscribe(A<IObserver<SmartMeterValue>>._))
            .ReturnsLazily(call => subject.Subscribe(call.GetArgument<IObserver<SmartMeterValue>>(0)!));

        var sut = new SmartMeterDataProducer(
            pipeline,
            NullLogger<SmartMeterDataProducer>.Instance,
            factory,
            Options.Create(new SmartMeterOptions()));

        return (sut, port, pipeline);
    }

    [Fact]
    public async Task StartAsync_OpensPortAndSubscribesToPipeline()
    {
        // Arrange
        var (sut, port, pipeline) = CreateSut();
        var observer = A.Fake<IObserver<SmartMeterValue>>();

        // Act
        await sut.StartAsync(observer);

        // Assert
        port.OpenCount.Should().Be(1);
        // Subscribe runs via SubscribeOn(TaskPoolScheduler), so the Subscribe call on
        // the fake may not have happened synchronously when the assertion runs. Poll
        // briefly to avoid CI flakiness.
        await WaitForCallAsync(pipeline,
            call => call.Method.Name == nameof(IObservable<SmartMeterValue>.Subscribe));
        A.CallTo(() => pipeline.Subscribe(A<IObserver<SmartMeterValue>>._)).MustHaveHappened();
    }

    private static async Task WaitForCallAsync(object fake, Func<ICompletedFakeObjectCall, bool> predicate,
        int timeoutMs = 2000)
    {
        var start = Environment.TickCount;

        while (Environment.TickCount - start < timeoutMs)
        {
            if (Fake.GetCalls(fake).Any(predicate))
            {
                return;
            }

            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task StartAsync_ForwardsSerialPortBytesToPipeline()
    {
        // Arrange
        var (sut, port, pipeline) = CreateSut();
        var observer = A.Fake<IObserver<SmartMeterValue>>();
        await sut.StartAsync(observer);

        // Act
        var payload = new byte[] { 1, 2, 3 };
        port.PushBytes(payload);

        // Assert
        A.CallTo(() => pipeline.OnNext(payload)).MustHaveHappened();
    }

    [Fact]
    public async Task StopAsync_ClosesPortAndDisposesSubscription()
    {
        // Arrange
        var (sut, port, pipeline) = CreateSut();
        var observer = A.Fake<IObserver<SmartMeterValue>>();
        await sut.StartAsync(observer);

        // Act
        await sut.StopAsync();
        // After stop, pushing bytes should not propagate any more.
        port.PushBytes([0xFF]);

        // Assert
        port.CloseCount.Should().Be(1);
        A.CallTo(() => pipeline.OnNext(A<byte[]>.That.Matches(b => b.Length == 1 && b[0] == 0xFF)))
            .MustNotHaveHappened();
    }

    [Fact]
    public void Dispose_WithoutStart_DisposesPortIdempotently()
    {
        // Arrange
        var (sut, port, _) = CreateSut();

        // Act
        sut.Dispose();
        sut.Dispose();

        // Assert - port disposed on both calls, but no throw on second call
        port.DisposeCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Dispose_AfterStart_DisposesSubscriptionAndPort()
    {
        // Arrange
        var (sut, port, _) = CreateSut();
        var observer = A.Fake<IObserver<SmartMeterValue>>();
        await sut.StartAsync(observer);

        // Act
        sut.Dispose();

        // Assert
        port.DisposeCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task StartAsync_CalledTwice_SubscribesToSerialPortOnlyOnce()
    {
        // Arrange
        var (sut, port, pipeline) = CreateSut();
        var observer = A.Fake<IObserver<SmartMeterValue>>();

        // Act
        await sut.StartAsync(observer);
        await sut.StartAsync(observer);

        port.PushBytes([0x01]);

        // Assert - pipeline.OnNext should still only receive one forward per pushed batch
        A.CallTo(() => pipeline.OnNext(A<byte[]>.That.Matches(b => b.Length == 1 && b[0] == 0x01)))
            .MustHaveHappenedOnceExactly();
    }
}

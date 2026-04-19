using AwesomeAssertions;
using CreativeCoders.SmartMeter.Core.SmlData;
using CreativeCoders.SmartMeter.DataProcessing;
using CreativeCoders.SmartMeter.Server.Core;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CreativeCoders.SmartMeter.Server.Core.Tests;

public class SmartMeterServerTests
{
    private readonly IMqttValuePublisher _publisher = A.Fake<IMqttValuePublisher>();
    private readonly ISmartMeterDataProducer _producer = A.Fake<ISmartMeterDataProducer>();

    private SmartMeterServer CreateSut() =>
        new(NullLogger<SmartMeterServer>.Instance, _publisher, _producer);

    [Fact]
    public async Task StartAsync_InitializesPublisherThenStartsProducerWithPublisher()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.StartAsync();

        // Assert
        A.CallTo(() => _publisher.InitAsync()).MustHaveHappened()
            .Then(A.CallTo(() => _producer.StartAsync(_publisher)).MustHaveHappened());
    }

    [Fact]
    public async Task StartAsync_WhenPublisherInitFails_DoesNotStartProducer()
    {
        // Arrange
        A.CallTo(() => _publisher.InitAsync()).Throws<InvalidOperationException>();
        var sut = CreateSut();

        // Act
        var act = () => sut.StartAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        A.CallTo(() => _producer.StartAsync(A<IObserver<SmartMeterValue>>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task StopAsync_StopsDataProducerThenDisposesPublisher()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.StopAsync();

        // Assert
        A.CallTo(() => _producer.StopAsync()).MustHaveHappened()
            .Then(A.CallTo(() => _publisher.DisposeAsync()).MustHaveHappened());
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        // Act
        var act = () => new SmartMeterServer(null!, _publisher, _producer);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullPublisher_Throws()
    {
        // Act
        var act = () => new SmartMeterServer(NullLogger<SmartMeterServer>.Instance, null!, _producer);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullProducer_Throws()
    {
        // Act
        var act = () => new SmartMeterServer(
            NullLogger<SmartMeterServer>.Instance, _publisher, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}

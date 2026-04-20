using System.Globalization;
using System.Text;
using AwesomeAssertions;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using Xunit;

namespace CreativeCoders.SmartMeter.DataProcessing.Tests;

public class MqttValuePublisherTests : IAsyncLifetime
{
    private readonly IMqttClient _client = A.Fake<IMqttClient>();

    private readonly MqttPublisherOptions _options = new MqttPublisherOptions
    {
        Server = new Uri("tcp://localhost:1883"),
        ClientName = "test-client",
        TopicTemplate = "smartmeter/values/{0}"
    };

    private MqttValuePublisher? _sut;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_sut is not null)
        {
            await _sut.DisposeAsync();
            _sut = null;
        }
    }

    private static MqttClientConnectResult SuccessConnectResult() => new MqttClientConnectResult
        { ResultCode = MqttClientConnectResultCode.Success };

    private static MqttClientPublishResult PublishOk() =>
        new MqttClientPublishResult(null, MqttClientPublishReasonCode.Success, null!, []);

    private MqttValuePublisher Create()
    {
        _sut = new MqttValuePublisher(_options, NullLogger<MqttValuePublisher>.Instance, _client);
        return _sut;
    }

    [Fact]
    public async Task InitAsync_WhenConnectSucceeds_DoesNotThrow()
    {
        // Arrange
        A.CallTo(() => _client.ConnectAsync(A<MqttClientOptions>._, A<CancellationToken>._))
            .Returns(SuccessConnectResult());
        var sut = Create();

        // Act
        await sut.InitAsync();

        // Assert
        A.CallTo(() => _client.ConnectAsync(A<MqttClientOptions>._, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task InitAsync_WhenConnectFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var failed = new MqttClientConnectResult
        {
            ResultCode = MqttClientConnectResultCode.BadUserNameOrPassword,
            ReasonString = "nope"
        };
        A.CallTo(() => _client.ConnectAsync(A<MqttClientOptions>._, A<CancellationToken>._))
            .Returns(failed);
        var sut = Create();

        // Act
        var act = sut.InitAsync;

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BadUserNameOrPassword*");
    }

    [Fact]
    public async Task OnNext_AfterInit_PublishesValueAsJsonByDefault()
    {
        // Arrange
        A.CallTo(() => _client.ConnectAsync(A<MqttClientOptions>._, A<CancellationToken>._))
            .Returns(SuccessConnectResult());
        A.CallTo(() => _client.PublishAsync(A<MqttApplicationMessage>._, A<CancellationToken>._))
            .Returns(PublishOk());

        var sut = Create();
        await sut.InitAsync();

        // Act
        sut.OnNext(new SmartMeterValue(SmartMeterValueType.TotalPurchasedEnergy) { Value = 42m });

        // Assert - wait up to 2s for the worker thread to publish
        await WaitForAsync(() =>
            Fake.GetCalls(_client).Any(c => c.Method.Name == nameof(IMqttClient.PublishAsync)));

        var published = Fake.GetCalls(_client)
            .Where(c => c.Method.Name == nameof(IMqttClient.PublishAsync))
            .Select(c => (MqttApplicationMessage)c.Arguments[0]!)
            .ToList();

        published.Should().ContainSingle();
        published[0].Topic.Should().Be("smartmeter/values/TotalPurchasedEnergy");
        Encoding.UTF8.GetString(System.Buffers.BuffersExtensions.ToArray(published[0].Payload)).Should()
            .Contain("\"Value\":42");
    }

    [Fact]
    public async Task OnNext_WithWriteAsJsonFalse_PublishesRawInvariantDecimal()
    {
        // Arrange
        A.CallTo(() => _client.ConnectAsync(A<MqttClientOptions>._, A<CancellationToken>._))
            .Returns(SuccessConnectResult());
        A.CallTo(() => _client.PublishAsync(A<MqttApplicationMessage>._, A<CancellationToken>._))
            .Returns(PublishOk());

        var sut = Create();
        await sut.InitAsync();

        // Act
        sut.OnNext(new SmartMeterValue(SmartMeterValueType.GridPowerBalance)
        {
            Value = -12.5m,
            WriteAsJson = false
        });

        // Assert
        await WaitForAsync(() =>
            Fake.GetCalls(_client).Any(c => c.Method.Name == nameof(IMqttClient.PublishAsync)));

        var msg = Fake.GetCalls(_client)
            .Where(c => c.Method.Name == nameof(IMqttClient.PublishAsync))
            .Select(c => (MqttApplicationMessage)c.Arguments[0]!)
            .Single();

        msg.Topic.Should().Be("smartmeter/values/GridPowerBalance");
        Encoding.UTF8.GetString(System.Buffers.BuffersExtensions.ToArray(msg.Payload))
            .Should().Be((-12.5m).ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task OnNext_WhenPublishThrows_WorkerContinuesAndDoesNotCrash()
    {
        // Arrange
        A.CallTo(() => _client.ConnectAsync(A<MqttClientOptions>._, A<CancellationToken>._))
            .Returns(SuccessConnectResult());
        A.CallTo(() => _client.PublishAsync(A<MqttApplicationMessage>._, A<CancellationToken>._))
            .Throws<InvalidOperationException>().Once()
            .Then.Returns(PublishOk());

        var sut = Create();
        await sut.InitAsync();

        // Act
        sut.OnNext(new SmartMeterValue(SmartMeterValueType.TotalPurchasedEnergy) { Value = 1m });
        sut.OnNext(new SmartMeterValue(SmartMeterValueType.TotalSoldEnergy) { Value = 2m });

        // Assert - second publish should still be attempted
        await WaitForAsync(() =>
            Fake.GetCalls(_client).Count(c => c.Method.Name == nameof(IMqttClient.PublishAsync)) >= 2);

        Fake.GetCalls(_client).Count(c => c.Method.Name == nameof(IMqttClient.PublishAsync))
            .Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MqttValuePublisher(null!, NullLogger<MqttValuePublisher>.Instance, _client);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MqttValuePublisher(_options, null!, _client);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new MqttValuePublisher(_options, NullLogger<MqttValuePublisher>.Instance, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OnError_AndOnCompleted_DoNotThrow()
    {
        // Arrange
        var sut = Create();

        // Act
        var act1 = () => sut.OnError(new Exception("boom"));
        var act2 = sut.OnCompleted;

        // Assert
        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var start = Environment.TickCount;

        while (!condition())
        {
            if (Environment.TickCount - start > timeoutMs)
            {
                return;
            }

            await Task.Delay(20);
        }
    }
}

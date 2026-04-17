using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CreativeCoders.Core;
using CreativeCoders.Net;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class MqttValuePublisher : IObserver<SmartMeterValue>
{
    private readonly IMqttClient _client;

    private readonly ILogger<MqttValuePublisher> _logger;

    private readonly MqttPublisherOptions _options;

    private readonly BlockingCollection<SmartMeterValue> _publishingQueue;

    private readonly Thread _workerThread;

    public MqttValuePublisher(MqttPublisherOptions options, ILogger<MqttValuePublisher> logger)
    {
        _options = Ensure.NotNull(options);
        _logger = Ensure.NotNull(logger);

        _client = new MqttFactory().CreateMqttClient();

        _publishingQueue = new BlockingCollection<SmartMeterValue>();

        _workerThread = new Thread(async void () =>
        {
            try
            {
                await DoWorkAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in MqttValuePublisher worker thread");
            }
        });
    }

    public async Task InitAsync()
    {
        var connectResult = await _client
            .ConnectAsync(new MqttClientOptionsBuilder()
                .WithClientId(_options.ClientName)
                .WithConnectionUri(_options.Server)
                .Build());

        _client.DisconnectedAsync += _ => TryReconnectAsync();

        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            throw new InvalidOperationException(
                $"Mqtt connection failed with status: {connectResult.ResultCode}  '{connectResult.ReasonString}'");
        }

        _workerThread.Start();
    }

    private async Task TryReconnectAsync()
    {
        _logger.LogWarning("Mqtt client disconnected. Reconnecting...");

        await Task.Delay(1000);

        await _client.ConnectAsync(new MqttClientOptionsBuilder()
            .WithClientId(_options.ClientName)
            .WithConnectionUri(_options.Server)
            .Build());
    }

    private async Task DoWorkAsync()
    {
        foreach (var value in _publishingQueue.GetConsumingEnumerable())
        {
            _logger.LogDebug("Publish value: {ValueType} = {Value}", value.Type, value.Value);

            var payload = value.WriteAsJson
                ? JsonSerializer.Serialize(new { value.Value })
                : value.Value.ToString(CultureInfo.InvariantCulture);

            var message = new MqttApplicationMessage
            {
                Topic = string.Format(_options.TopicTemplate, value.Type),
                //ContentType = ContentMediaTypes.Application.Json,
                Payload = Encoding.UTF8.GetBytes(payload)
            };

            var publishResult = await SendMessageAsync(message);

            _logger.LogDebug("Publishing result: {ReasonCode}  {ReasonString}", publishResult.ReasonCode,
                publishResult.ReasonString);
        }
    }

    private async Task<MqttClientPublishResult> SendMessageAsync(MqttApplicationMessage message)
    {
        try
        {
            return await _client.PublishAsync(message).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending message");
        }

        return new MqttClientPublishResult(null, MqttClientPublishReasonCode.UnspecifiedError,
            "Sending message failed with exception", []);
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(SmartMeterValue value)
    {
        _publishingQueue.Add(value);
    }
}

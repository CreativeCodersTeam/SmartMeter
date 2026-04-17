using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CreativeCoders.Core;
using CreativeCoders.Daemon;
using CreativeCoders.SmartMeter.DataProcessing;
using CreativeCoders.SmartMeter.Sml.Reactive;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreativeCoders.SmartMeter.Server.Core;

[UsedImplicitly]
public class SmartMeterServer(
    ILogger<SmartMeterServer> logger,
    IOptions<MqttPublisherOptions> mqttPublisherOptions,
    ILoggerFactory loggerFactory)
    : IDaemonService
{
    private readonly ILoggerFactory _loggerFactory = Ensure.NotNull(loggerFactory);
    private readonly ILogger<SmartMeterServer> _logger = Ensure.NotNull(logger);
    private readonly MqttPublisherOptions _mqttPublisherOptions = mqttPublisherOptions.Value;
    private readonly ReactiveSerialPort _serialPort = new ReactiveSerialPort("/dev/ttyUSB0");

    private IDisposable? _subscription;

    private void CloseSerialPort()
    {
        _logger.LogInformation("Closing serial port...");
        _serialPort.Close();
        _logger.LogInformation("Serial port closed");
    }

    private void DisposingSubscription()
    {
        if (_subscription == null)
        {
            return;
        }

        _logger.LogInformation("Disposing subscription...");

        _subscription.Dispose();

        _logger.LogInformation("Subscription disposed");

        _subscription = null;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting SmartMeter server");

        var mqttValuePublisher =
            new MqttValuePublisher(_mqttPublisherOptions, _loggerFactory.CreateLogger<MqttValuePublisher>());

        await mqttValuePublisher.InitAsync();

        _subscription ??= _serialPort
            .SelectSmlMessages()
            .SelectSmlValues()
            .SelectSmartMeterValues()
            .SubscribeOn(new TaskPoolScheduler(new TaskFactory()))
            .Subscribe(mqttValuePublisher);

        _serialPort.Open();
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Stopping SmartMeter server");

        DisposingSubscription();

        CloseSerialPort();

        _logger.LogInformation("SmartMeter server stopped");

        return Task.CompletedTask;
    }
}

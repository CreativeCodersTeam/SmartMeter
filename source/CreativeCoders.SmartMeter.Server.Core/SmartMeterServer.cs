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
    ILoggerFactory loggerFactory,
    ISmartMeterDataProducer smartMeterDataProducer)
    : IDaemonService
{
    private readonly ISmartMeterDataProducer _smartMeterDataProducer = Ensure.NotNull(smartMeterDataProducer);
    private readonly ILoggerFactory _loggerFactory = Ensure.NotNull(loggerFactory);
    private readonly ILogger<SmartMeterServer> _logger = Ensure.NotNull(logger);
    private readonly MqttPublisherOptions _mqttPublisherOptions = mqttPublisherOptions.Value;

    private IDisposable? _subscription;

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting SmartMeter server");

        var mqttValuePublisher =
            new MqttValuePublisher(_mqttPublisherOptions, _loggerFactory.CreateLogger<MqttValuePublisher>());

        await mqttValuePublisher.InitAsync();

        await _smartMeterDataProducer.StartAsync(mqttValuePublisher);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping SmartMeter server");

        await _smartMeterDataProducer.StopAsync();

        _logger.LogInformation("SmartMeter server stopped");
    }
}

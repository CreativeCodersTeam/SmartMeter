using CreativeCoders.Core;
using CreativeCoders.Daemon;
using CreativeCoders.SmartMeter.Core.SmlData;
using CreativeCoders.SmartMeter.DataProcessing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMeter.Server.Core;

[UsedImplicitly]
public class SmartMeterServer(
    ILogger<SmartMeterServer> logger,
    IMqttValuePublisher mqttValuePublisher,
    ISmartMeterDataProducer smartMeterDataProducer)
    : IDaemonService
{
    private readonly ISmartMeterDataProducer _smartMeterDataProducer = Ensure.NotNull(smartMeterDataProducer);
    private readonly IMqttValuePublisher _mqttValuePublisher = Ensure.NotNull(mqttValuePublisher);
    private readonly ILogger<SmartMeterServer> _logger = Ensure.NotNull(logger);

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting SmartMeter server");

        await _mqttValuePublisher.InitAsync().ConfigureAwait(false);

        await _smartMeterDataProducer.StartAsync(_mqttValuePublisher).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping SmartMeter server");

        await _smartMeterDataProducer.StopAsync().ConfigureAwait(false);

        _logger.LogInformation("SmartMeter server stopped");
    }
}

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
public class SmartMeterServer : IDaemonService
{
    private readonly ILogger<MqttValuePublisher> _publisherLogger;
    private readonly ILogger<SmartMeterServer> _logger;
    
    private IDisposable? _subscription;

    private readonly ReactiveSerialPort _serialPort;
    
    private readonly MqttPublisherOptions _mqttPublisherOptions;

    public SmartMeterServer(ILogger<SmartMeterServer> logger,
        IOptions<MqttPublisherOptions> mqttPublisherOptions,
        ILogger<MqttValuePublisher> publisherLogger)
    {
        _logger = Ensure.NotNull(logger, nameof(logger));
        _mqttPublisherOptions = mqttPublisherOptions.Value;
        _publisherLogger = Ensure.NotNull(publisherLogger, nameof(publisherLogger));
        
        _serialPort = new ReactiveSerialPort("/dev/ttyUSB0");
    }
    
    public async Task StartAsync()
    {
        _logger.LogInformation("Starting SmartMeter server");

        var mqttValuePublisher = new MqttValuePublisher(_mqttPublisherOptions, _publisherLogger);

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
        
        _serialPort.Close();
        
        if (_subscription != null)
        {
            _subscription.Dispose();
            _subscription = null;
        }

        return Task.CompletedTask;
    }
}

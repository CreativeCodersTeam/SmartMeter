using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CreativeCoders.Core;
using CreativeCoders.Daemon;
using CreativeCoders.SmartMeter.DataProcessing;
using CreativeCoders.SmartMeter.Sml.Reactive;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMeter.Server.Core;

[UsedImplicitly]
public class SmartMeterServer : IDaemonService
{
    private readonly ILogger<SmartMeterServer> _logger;
    
    private IDisposable? _subscription;

    private readonly ReactiveSerialPort _serialPort;

    public SmartMeterServer(ILogger<SmartMeterServer> logger)
    {
        _logger = Ensure.NotNull(logger, nameof(logger));
        
        _serialPort = new ReactiveSerialPort("/dev/ttyUSB0");
    }
    
    public Task StartAsync()
    {
        _logger.LogInformation("Starting SmartMeter server");
        
        _subscription ??= _serialPort
            .SelectSmlMessages()
            .SelectSmlValues()
            .SelectSmartMeterValues()
            .SubscribeOn(new TaskPoolScheduler(new TaskFactory()))
            .Subscribe(value => { Console.WriteLine($"{value.Type}:  {value.Value}"); });

        _serialPort.Open();

        return Task.CompletedTask;
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

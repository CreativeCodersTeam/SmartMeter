using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CreativeCoders.Core;
using CreativeCoders.SmartMeter.DataProcessing;
using CreativeCoders.SmartMeter.Sml.Reactive;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMeter.Server.Core;

public class SmartMeterDataProducer(ILogger<SmartMeterDataProducer> logger) : ISmartMeterDataProducer
{
    private readonly ILogger<SmartMeterDataProducer> _logger = Ensure.NotNull(logger);
    private readonly ReactiveSerialPort _serialPort = new ReactiveSerialPort("/dev/ttyUSB0");

    private IDisposable? _subscription;

    public Task StartAsync(IObserver<SmartMeterValue> observer)
    {
        _logger.LogInformation("Starting SmartMeter data producer");

        _subscription ??= _serialPort
            .Do(_ => _logger.LogDebug("Data received from serial port"))
            .SelectSmlMessages()
            .SelectSmlValues()
            .SelectSmartMeterValues()
            .SubscribeOn(new TaskPoolScheduler(new TaskFactory()))
            .Subscribe(observer);

        _logger.LogInformation("SmartMeter data producer started");

        _logger.LogInformation("Opening serial port...");
        _serialPort.Open();
        _logger.LogInformation("Serial port opened");

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Stopping SmartMeter data producer");

        DisposingSubscription();

        CloseSerialPort();

        _logger.LogInformation("SmartMeter data producer stopped");

        return Task.CompletedTask;
    }

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

        _logger.LogInformation("Disposing data producer subscription...");

        _subscription.Dispose();

        _logger.LogInformation("Subscription data producer disposed");

        _subscription = null;
    }
}

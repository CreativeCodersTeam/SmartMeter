using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CreativeCoders.Core;
using CreativeCoders.SmartMeter.DataProcessing;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMeter.Core.SmlData;

public sealed class SmartMeterDataProducer(
    ISmartMeterReactiveDataPipeline reactiveDataPipeline,
    ILogger<SmartMeterDataProducer> logger) : ISmartMeterDataProducer
{
    private readonly ISmartMeterReactiveDataPipeline _reactiveDataPipeline = Ensure.NotNull(reactiveDataPipeline);
    private readonly ILogger<SmartMeterDataProducer> _logger = Ensure.NotNull(logger);
    private readonly ReactiveSerialPort _serialPort = new ReactiveSerialPort("/dev/ttyUSB0");

    private IDisposable? _subscription;

    public Task StartAsync(IObserver<SmartMeterValue> observer)
    {
        _logger.LogInformation("Starting SmartMeter data producer");

        _reactiveDataPipeline
            .SubscribeOn(new TaskPoolScheduler(new TaskFactory()))
            .Subscribe(observer);

        _subscription ??= _serialPort
            .Subscribe(_reactiveDataPipeline);

        _logger.LogInformation("SmartMeter data producer initialized");

        OpenSerialPort();

        return Task.CompletedTask;
    }

    private void OpenSerialPort()
    {
        _logger.LogInformation("Opening serial port...");
        _serialPort.Open();
        _logger.LogInformation("Serial port opened");
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

    public void Dispose()
    {
        _serialPort.Dispose();

        if (_subscription == null)
        {
            return;
        }

        _logger.LogDebug("Disposing subscription...");
        _subscription.Dispose();
        _logger.LogDebug("Subscription disposed");

        _subscription = null;
    }
}

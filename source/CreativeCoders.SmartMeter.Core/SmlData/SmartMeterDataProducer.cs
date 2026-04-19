using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CreativeCoders.Core;
using CreativeCoders.SmartMeter.DataProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreativeCoders.SmartMeter.Core.SmlData;

public sealed class SmartMeterDataProducer : ISmartMeterDataProducer
{
    private readonly ISmartMeterReactiveDataPipeline _reactiveDataPipeline;
    private readonly ILogger<SmartMeterDataProducer> _logger;
    private readonly IReactiveSerialPort _serialPort;

    private IDisposable? _subscription;

    public SmartMeterDataProducer(
        ISmartMeterReactiveDataPipeline reactiveDataPipeline,
        ILogger<SmartMeterDataProducer> logger,
        IReactiveSerialPortFactory serialPortFactory,
        IOptions<SmartMeterOptions> smartMeterOptions)
    {
        _reactiveDataPipeline = Ensure.NotNull(reactiveDataPipeline);
        _logger = Ensure.NotNull(logger);
        Ensure.NotNull(serialPortFactory);
        var options = Ensure.NotNull(smartMeterOptions).Value;

        _serialPort = serialPortFactory.Create(options.PortName);
    }

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

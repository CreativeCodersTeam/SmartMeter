using CreativeCoders.SmartMeter.DataProcessing;

namespace CreativeCoders.SmartMeter.Server.Core;

public interface ISmartMeterDataProducer
{
    Task StartAsync(IObserver<SmartMeterValue> observer);

    Task StopAsync();
}

using CreativeCoders.SmartMeter.DataProcessing;

namespace CreativeCoders.SmartMeter.Core.SmlData;

public interface ISmartMeterDataProducer : IDisposable
{
    Task StartAsync(IObserver<SmartMeterValue> observer);

    Task StopAsync();
}

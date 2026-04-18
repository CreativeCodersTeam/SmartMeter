using CreativeCoders.SmartMeter.DataProcessing;

namespace CreativeCoders.SmartMeter.Server.Core.SmlData;

public interface ISmartMeterDataProducer : IDisposable
{
    Task StartAsync(IObserver<SmartMeterValue> observer);

    Task StopAsync();
}

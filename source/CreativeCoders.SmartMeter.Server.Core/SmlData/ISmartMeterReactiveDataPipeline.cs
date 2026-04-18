using CreativeCoders.SmartMeter.DataProcessing;

namespace CreativeCoders.SmartMeter.Server.Core.SmlData;

public interface ISmartMeterReactiveDataPipeline : IObserver<byte[]>, IObservable<SmartMeterValue>
{
}

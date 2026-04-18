using CreativeCoders.SmartMeter.DataProcessing;

namespace CreativeCoders.SmartMeter.Server.Core;

public interface ISmartMeterReactiveDataPipeline : IObserver<byte[]>, IObservable<SmartMeterValue>
{
}

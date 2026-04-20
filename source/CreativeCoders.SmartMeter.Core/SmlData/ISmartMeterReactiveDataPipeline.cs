using CreativeCoders.SmartMeter.DataProcessing;

namespace CreativeCoders.SmartMeter.Core.SmlData;

public interface ISmartMeterReactiveDataPipeline : IObserver<byte[]>, IObservable<SmartMeterValue>;

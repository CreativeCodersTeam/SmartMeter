using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public static class SmartMeterReactiveExtensions
{
    public static IObservable<SmartMeterValue> SelectSmartMeterValues(this IObservable<SmlValue> observable,
        TimeProvider? timeProvider = null)
    {
        return new SmlValueProcessor(observable, timeProvider);
    }
}

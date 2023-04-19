using System.Reactive.Subjects;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmlValueProcessor : IObservable<SmartMeterValue>
{
    private readonly Subject<SmartMeterValue> _valueSubject;

    private ValueHistory _valueHistory;
    
    public SmlValueProcessor(IObservable<SmlValue> observable)
    {
        _valueSubject = new Subject<SmartMeterValue>();

        _valueHistory = new ValueHistory();

        observable.Subscribe(ProcessValue);
    }

    private void ProcessValue(SmlValue smlValue)
    {
        var historyData = _valueHistory.GetHistoryData(smlValue.ValueType);

        if (historyData.LastValue == null || historyData.LastValue.Value != smlValue.Value)
        {
            
        }
    }
    
    public IDisposable Subscribe(IObserver<SmartMeterValue> observer)
    {
        return _valueSubject.Subscribe(observer);
    }
}

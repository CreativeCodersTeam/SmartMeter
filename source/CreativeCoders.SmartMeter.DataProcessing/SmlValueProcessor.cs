using System.Reactive.Linq;
using System.Reactive.Subjects;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmlValueProcessor : IObservable<SmartMeterValue>
{
    private readonly ValueHistory _valueHistory;
    private readonly Subject<SmartMeterValue> _valueSubject;

    public SmlValueProcessor(IObservable<SmlValue> observable)
    {
        _valueSubject = new Subject<SmartMeterValue>();

        _valueHistory = new ValueHistory();

        observable
            .Select(Observable.Return)
            .Concat()
            .Subscribe(ProcessValue);
    }

    private void ProcessValue(SmlValue smlValue)
    {
        var historyData = _valueHistory.GetHistoryData(smlValue.ValueType);

        CalcCurrentPower(smlValue, historyData);

        var now = DateTimeOffset.Now;

        historyData.DataSets.Add(new ValueHistoryDataSet(smlValue) { TimeStamp = now });

        var timeDiff = now - (historyData.LastValueTimeStamp ?? DateTimeOffset.MinValue);

        if ((historyData.LastValue?.Value == smlValue.Value ||
             timeDiff.TotalSeconds < 30) && timeDiff.TotalMinutes < 5)
        {
            return;
        }

        _valueSubject.OnNext(CreateTotalSmartMeterValue(smlValue));

        historyData.LastValue = smlValue;
        historyData.LastValueTimeStamp = now;
    }

    private void CalcCurrentPower(SmlValue smlValue, ValueHistoryData historyData)
    {
        foreach (var dataSet in historyData.DataSets)
        {
            var valueDiff = smlValue.Value - dataSet.Value.Value;
            var timeDiff = DateTimeOffset.Now - dataSet.TimeStamp;

            if (valueDiff > 10 || timeDiff > TimeSpan.FromSeconds(20))
            {
                historyData.DataSets.Clear();

                var mp = TimeSpan.FromHours(1).TotalMilliseconds / timeDiff.TotalMilliseconds;

                var value = (decimal)((double)valueDiff * mp);
                value = Math.Round(value, 0);

                _valueSubject.OnNext(CreateCurrentSmartMeterValue(smlValue.ValueType, value));

                break;
            }
        }
    }

    private static SmartMeterValue CreateCurrentSmartMeterValue(SmlValueType smlValueType, decimal value)
    {
        return smlValueType switch
        {
            SmlValueType.PurchasedEnergy => new SmartMeterValue(SmartMeterValueType.CurrentPurchasingPower)
            {
                Value = value
            },
            SmlValueType.SoldEnergy => new SmartMeterValue(SmartMeterValueType.CurrentSellingPower)
            {
                Value = value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(smlValueType))
        };
    }

    private static SmartMeterValue CreateTotalSmartMeterValue(SmlValue smlValue)
    {
        return smlValue.ValueType switch
        {
            SmlValueType.PurchasedEnergy => new SmartMeterValue(SmartMeterValueType.TotalPurchasedEnergy)
            {
                Value = smlValue.Value
            },
            SmlValueType.SoldEnergy => new SmartMeterValue(SmartMeterValueType.TotalSoldEnergy)
            {
                Value = smlValue.Value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(smlValue.ValueType))
        };
    }

    public IDisposable Subscribe(IObserver<SmartMeterValue> observer)
    {
        return _valueSubject
            .Subscribe(observer);
    }
}

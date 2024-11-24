using System.Reactive.Linq;
using System.Reactive.Subjects;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmlValueProcessor : IObservable<SmartMeterValue>
{
    private readonly TimeProvider _timeProvider;

    private readonly ValueHistory _valueHistory = new ValueHistory();

    private readonly Subject<SmartMeterValue> _valueSubject = new Subject<SmartMeterValue>();

    public SmlValueProcessor(IObservable<SmlValue> observable, TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;

        observable
            .Select(Observable.Return)
            .Concat()
            .Subscribe(ProcessValue, () => _valueSubject.OnCompleted());
    }

    private void ProcessValue(SmlValue smlValue)
    {
        var historyData = _valueHistory.GetHistoryData(smlValue.ValueType);

        CalcCurrentPower(smlValue, historyData);

        var now = _timeProvider.GetUtcNow();

        historyData.DataSets.Add(new ValueHistoryDataSet(smlValue) { TimeStamp = now });

        var timeDiff = now - (historyData.LastValueTimeStamp ?? DateTimeOffset.MinValue);

        if ((historyData.LastValue?.Value == smlValue.Value ||
             timeDiff.TotalSeconds < 30) && timeDiff.TotalMinutes < 5)
        {
            return;
        }

        _valueSubject.OnNext(CreateTotalSmartMeterValue(smlValue.ValueType, smlValue.Value));

        historyData.LastValue = smlValue;
        historyData.LastValueTimeStamp = now;
    }

    private void CalcCurrentPower(SmlValue smlValue, ValueHistoryData historyData)
    {
        foreach (var dataSet in historyData.DataSets)
        {
            var valueDiff = smlValue.Value - dataSet.Value.Value;
            var timeDiff = _timeProvider.GetUtcNow() - dataSet.TimeStamp;

            if (valueDiff > 10 || timeDiff > TimeSpan.FromSeconds(20))
            {
                historyData.DataSets.Clear();

                var mp = TimeSpan.FromHours(1).TotalMilliseconds / timeDiff.TotalMilliseconds;

                var value = (decimal)((double)valueDiff * mp);
                value = Math.Round(value, 0);

                PushNewCurrentValue(CreateCurrentSmartMeterValue(smlValue.ValueType, value));

                break;
            }
        }
    }

    private void PushNewCurrentValue(SmartMeterValue value)
    {
        _valueSubject.OnNext(value);

        switch (value.Type)
        {
            case SmartMeterValueType.CurrentPurchasingPower:
                _valueSubject.OnNext(new SmartMeterValue(SmartMeterValueType.GridPowerBalance)
                {
                    Value = value.Value * -1
                });
                break;
            case SmartMeterValueType.CurrentSellingPower:
                _valueSubject.OnNext(new SmartMeterValue(SmartMeterValueType.GridPowerBalance)
                {
                    Value = value.Value
                });
                break;
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

    private static SmartMeterValue CreateTotalSmartMeterValue(SmlValueType smlValueType, decimal value)
    {
        return smlValueType switch
        {
            SmlValueType.PurchasedEnergy => new SmartMeterValue(SmartMeterValueType.TotalPurchasedEnergy)
            {
                Value = value
            },
            SmlValueType.SoldEnergy => new SmartMeterValue(SmartMeterValueType.TotalSoldEnergy)
            {
                Value = value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(smlValueType))
        };
    }

    public IDisposable Subscribe(IObserver<SmartMeterValue> observer)
    {
        return _valueSubject
            .Subscribe(observer);
    }
}

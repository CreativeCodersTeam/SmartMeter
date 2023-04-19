﻿using System.Reactive.Linq;
using System.Reactive.Subjects;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmlValueProcessor : IObservable<SmartMeterValue>
{
    private readonly Subject<SmartMeterValue> _valueSubject;

    private readonly ValueHistory _valueHistory;
    
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
        
        if (historyData.LastValue == null || historyData.LastValue.Value != smlValue.Value)
        {
            _valueSubject.OnNext(CreateTotalSmartMeterValue(smlValue));
            
            historyData.DataSets.Add(new ValueHistoryDataSet(smlValue){TimeStamp = DateTimeOffset.Now});
        }

        historyData.LastValue = smlValue;
    }

    private void CalcCurrentPower(SmlValue smlValue, ValueHistoryData historyData)
    {
        foreach (var dataSet in historyData
                     .DataSets
                     .Reverse())
        {
            var valueDiff = smlValue.Value - dataSet.Value.Value;
            var timeDiff = DateTimeOffset.Now - dataSet.TimeStamp;

            if (valueDiff > 2 || timeDiff > TimeSpan.FromSeconds(20))
            {
                historyData.DataSets.Clear();
                
                var mp = TimeSpan.FromHours(1).TotalMilliseconds / timeDiff.TotalMilliseconds;
                    
                var value = (decimal)((double)valueDiff * mp);
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
            _ => throw new ArgumentOutOfRangeException()
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
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public IDisposable Subscribe(IObserver<SmartMeterValue> observer)
    {
        return _valueSubject.Subscribe(observer);
    }
}

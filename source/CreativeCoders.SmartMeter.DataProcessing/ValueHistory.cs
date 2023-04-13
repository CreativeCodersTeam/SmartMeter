using System.Collections.Concurrent;
using CreativeCoders.Core;
using CreativeCoders.Core.Threading;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class ValueHistory
{
    private ConcurrentDictionary<SmartMeterValueType, IList<ValueHistoryData>> _data;

    public ValueHistory()
    {
        _data = new ConcurrentDictionary<SmartMeterValueType, IList<ValueHistoryData>>();
    }

    private IList<ValueHistoryData> GetHistoryData(SmartMeterValueType valueType)
    {
        lock (this)
        {
            if (_data.TryGetValue(valueType, out var dataList))
            {
                return dataList;
            }

            var newList = new ConcurrentList<ValueHistoryData>();

            _data[valueType] = newList;

            return newList;
        }
    }
    
    
}

public class ValueHistoryData
{
    public ValueHistoryData(SmartMeterValue value)
    {
        Value = Ensure.NotNull(value, nameof(value));
    }

    public DateTimeOffset TimeStamp { get; set; }

    public SmartMeterValue Value { get; }
}

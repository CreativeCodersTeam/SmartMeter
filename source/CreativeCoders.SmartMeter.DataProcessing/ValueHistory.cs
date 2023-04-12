using System.Collections.Concurrent;
using CreativeCoders.Core.Threading;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class ValueHistory
{
    private ConcurrentDictionary<SmartMeterValueType, ValueHistoryData> _items;

    public ValueHistory()
    {
        //_items = new ConcurrentList<ValueHistoryData>();
    }
    
    
}

public class ValueHistoryData
{
    public DateTimeOffset TimeStamp { get; set; }

    public SmartMeterValue Value { get; set; }
}

using System.Collections.Concurrent;
using CreativeCoders.Core;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class ValueHistory
{
    private readonly ConcurrentDictionary<SmlValueType, ValueHistoryData> _data;

    public ValueHistory()
    {
        _data = new ConcurrentDictionary<SmlValueType, ValueHistoryData>();
    }

    public ValueHistoryData GetHistoryData(SmlValueType valueType)
    {
        lock (this)
        {
            if (_data.TryGetValue(valueType, out var dataList))
            {
                return dataList;
            }

            var historyData = new ValueHistoryData();

            _data[valueType] = historyData;

            return historyData;
        }
    }
}

public class ValueHistoryData
{
    public ValueHistoryData()
    {
        DataSets = new List<ValueHistoryDataSet>();
    }

    public IList<ValueHistoryDataSet> DataSets { get; }

    public SmlValue? LastValue { get; set; }
}

public class ValueHistoryDataSet
{
    public ValueHistoryDataSet(SmlValue value)
    {
        Value = Ensure.NotNull(value, nameof(value));
    }

    public DateTimeOffset TimeStamp { get; init; }

    public SmlValue Value { get; }
}

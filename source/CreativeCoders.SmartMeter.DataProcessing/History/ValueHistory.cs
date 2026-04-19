using System.Collections.Concurrent;

namespace CreativeCoders.SmartMeter.DataProcessing.History;

public class ValueHistory
{
    private readonly Lock _syncObj = new Lock();

    private readonly ConcurrentDictionary<SmlValueType, ValueHistoryData> _data =
        new ConcurrentDictionary<SmlValueType, ValueHistoryData>();

    public ValueHistoryData GetHistoryData(SmlValueType valueType)
    {
        lock (_syncObj)
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

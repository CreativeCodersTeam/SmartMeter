﻿using System.Collections.Concurrent;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class ValueHistory
{
    private readonly ConcurrentDictionary<SmlValueType, ValueHistoryData> _data = new ConcurrentDictionary<SmlValueType, ValueHistoryData>();

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
using CreativeCoders.Core;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class ValueHistoryDataSet
{
    public ValueHistoryDataSet(SmlValue value)
    {
        Value = Ensure.NotNull(value, nameof(value));
    }

    public DateTimeOffset TimeStamp { get; init; }

    public SmlValue Value { get; }
}
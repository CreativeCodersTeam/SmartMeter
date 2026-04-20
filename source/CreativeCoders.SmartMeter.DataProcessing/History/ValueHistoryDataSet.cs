using CreativeCoders.Core;

namespace CreativeCoders.SmartMeter.DataProcessing.History;

public class ValueHistoryDataSet(SmlValue value)
{
    public DateTimeOffset TimeStamp { get; init; }

    public SmlValue Value { get; } = Ensure.NotNull(value);
}

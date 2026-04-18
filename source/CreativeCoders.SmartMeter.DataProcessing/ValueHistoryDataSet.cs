using CreativeCoders.Core;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class ValueHistoryDataSet(SmlValue value)
{
    public DateTimeOffset TimeStamp { get; init; }

    public SmlValue Value { get; } = Ensure.NotNull(value);
}

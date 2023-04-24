using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class ValueHistoryData
{
    public ValueHistoryData()
    {
        DataSets = new List<ValueHistoryDataSet>();
    }

    public IList<ValueHistoryDataSet> DataSets { get; }

    public SmlValue? LastValue { get; set; }

    public DateTimeOffset? LastValueTimeStamp { get; set; }
}
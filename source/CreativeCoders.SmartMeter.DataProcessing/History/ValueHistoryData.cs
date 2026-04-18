namespace CreativeCoders.SmartMeter.DataProcessing.History;

public class ValueHistoryData
{
    public IList<ValueHistoryDataSet> DataSets { get; } = new List<ValueHistoryDataSet>();

    public SmlValue? LastValue { get; set; }

    public DateTimeOffset? LastValueTimeStamp { get; set; }
}
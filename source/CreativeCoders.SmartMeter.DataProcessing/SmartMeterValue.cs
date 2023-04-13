namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmartMeterValue
{
    public SmartMeterValue(SmartMeterValueType type)
    {
        Type = type;
    }

    public SmartMeterValueType Type { get; }
    
    public decimal Value { get; set; }
}
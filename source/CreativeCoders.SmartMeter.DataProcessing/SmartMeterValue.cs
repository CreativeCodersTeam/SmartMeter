namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmartMeterValue(SmartMeterValueType type)
{
    public SmartMeterValueType Type { get; } = type;

    public decimal Value { get; init; }

    public bool WriteAsJson { get; set; } = true;
}

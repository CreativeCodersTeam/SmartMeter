namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmlValue
{
    public SmlValue(SmlValueType valueType)
    {
        ValueType = valueType;
    }

    public decimal Value { get; init; }

    public SmlValueType ValueType { get; }
}

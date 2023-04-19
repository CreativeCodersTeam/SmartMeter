namespace CreativeCoders.SmartMeter.Sml;

public class SmlValue
{
    public SmlValue(SmlValueType valueType)
    {
        ValueType = valueType;
    }

    public decimal Value { get; init; }

    public SmlValueType ValueType { get; }
}
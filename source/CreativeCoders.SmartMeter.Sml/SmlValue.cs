namespace CreativeCoders.SmartMeter.Sml;

public class SmlValue
{
    public SmlValue(SmlValueType valueType)
    {
        ValueType = valueType;
    }

    public decimal Value { get; set; }

    public SmlValueType ValueType { get; }
}
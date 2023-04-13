namespace CreativeCoders.SmartMeter.Sml;

public interface ISmlValueReader
{
    IEnumerable<SmlValue> Read(byte[] data);
}

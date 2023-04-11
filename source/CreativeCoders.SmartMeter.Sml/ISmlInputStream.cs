namespace CreativeCoders.SmartMeter.Sml;

public interface ISmlInputStream
{
    void AddNewData(byte[] data);
    void HandleSmlMessages(Action<SmlMessage> handleMessage);
}

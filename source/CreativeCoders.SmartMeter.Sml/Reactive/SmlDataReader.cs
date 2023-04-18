namespace CreativeCoders.SmartMeter.Sml.Reactive;

public class SmlDataReader
{
    private static readonly byte[] DocBeginSeq = {
        EscapeChar, EscapeChar, EscapeChar, EscapeChar,
        DocBeginChar, DocBeginChar, DocBeginChar, DocBeginChar
    };
    
    private const byte EscapeChar = 0x1B;

    private const byte DocBeginChar = 0x01;
    
    private readonly List<byte> _currentBlock = new List<byte>();

    private readonly List<byte> _buffer = new List<byte>();

    private SmlReadDataMode _currentMode = SmlReadDataMode.WaitForBegin;

    private Action<SmlMessage> _handleMessage;

    public void AddHandler(Action<SmlMessage> handleMessage)
    {
        _handleMessage = handleMessage;
    }

    public void Parse(byte[] data)
    {
        
    }
}

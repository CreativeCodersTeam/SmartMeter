using CreativeCoders.Core.Collections;

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

    private Action<SmlMessage> _handleMessage = _ => { };

    private SmlMessage? _currentMessage;

    public void AddHandler(Action<SmlMessage> handleMessage)
    {
        _handleMessage = handleMessage;
    }

    public void Parse(IEnumerable<byte> data)
    {
        data.ForEach(b =>
        {
            switch (_currentMode)
            {
                case SmlReadDataMode.WaitForBegin:
                    if (b != EscapeChar && b != DocBeginChar)
                    {
                        _buffer.Clear();
                        break;
                    }
                    _buffer.Add(b);
                    if (_buffer.Count == 8)
                    {
                        if (_buffer.SequenceEqual(DocBeginSeq))
                        {
                            _currentMode = SmlReadDataMode.InData;
                            _currentBlock.Clear();
                            _buffer.Clear();
                            _currentMode = SmlReadDataMode.InData;
                            break;
                        }
                        _buffer.Clear();
                        break;
                    }

                    if (_buffer.Count > 8)
                    {
                        _buffer.Clear();
                    }
                    
                    break;
                case SmlReadDataMode.InData:
                    if (b != EscapeChar)
                    {
                        _currentBlock.AddRange(_buffer);
                        _buffer.Clear();
                        _currentBlock.Add(b);
                        break;
                    }
                    
                    _buffer.Add(b);

                    if (_buffer.Count == 4)
                    {
                        _currentMessage = new SmlMessage(_currentBlock.ToArray());
                        _buffer.Clear();
                        _currentBlock.Clear();
                        _currentMode = SmlReadDataMode.ReadDataEnd;
                    }
                    
                    break;
                case SmlReadDataMode.ReadDataEnd:
                    _buffer.Add(b);

                    if (_buffer.Count == 4)
                    {
                        if (_currentMessage != null)
                        {
                            _currentMessage.FillByteCount = _buffer[1];
                            _currentMessage.Crc16Checksum = BitConverter.ToUInt16(_buffer.ToArray(), 2);
                            _buffer.Clear();
                            _handleMessage(_currentMessage);
                            _currentMessage = null;
                        }
                        
                        _currentMode = SmlReadDataMode.WaitForBegin;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_currentMode), "Unknown parser mode");
            }
        });
    }
}

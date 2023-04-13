using System.IO.Ports;
using CreativeCoders.Core;

namespace CreativeCoders.SmartMeter.Sml;

public class SmlSerialPortTransport : ISmlSerialPortTransport
{
    private readonly ISmlInputStream _smlInputStream;
    
    private readonly SerialPort _serialPort;
    
    public SmlSerialPortTransport(ISmlInputStream smlInputStream)
    {
        _smlInputStream = Ensure.NotNull(smlInputStream, nameof(smlInputStream));
        
        _serialPort = new SerialPort("/dev/ttyUSB0");
        _serialPort.DataReceived += SerialPortOnDataReceived;
    }

    public void StartProcessing()
    {
        _serialPort.Open();
    }

    public void StopProcessing()
    {
        _serialPort.Close();
    }

    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        var buffer = new byte[1024];

        var bytesToRead = Math.Min(buffer.Length, _serialPort.BytesToRead);

        var readBytes = _serialPort.Read(buffer, 0, bytesToRead);
        
        _smlInputStream.AddNewData(buffer.Take(readBytes).ToArray());
    }
}
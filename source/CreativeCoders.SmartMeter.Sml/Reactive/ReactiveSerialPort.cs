using System.IO.Ports;
using System.Reactive.Linq;
using CreativeCoders.Core;

namespace CreativeCoders.SmartMeter.Sml.Reactive;

public sealed class ReactiveSerialPort : IObservable<byte[]>, IDisposable
{
    private readonly IObservable<byte[]> _dataObservable;
    private readonly SerialPort _serialPort;

    public ReactiveSerialPort(string portName) : this(new SerialPort(portName))
    {
    }

    public ReactiveSerialPort(SerialPort serialPort)
    {
        _serialPort = Ensure.NotNull(serialPort, nameof(serialPort));

        _dataObservable = Observable
            .FromEvent<SerialDataReceivedEventHandler, SerialDataReceivedEventArgs>(
                handler =>
                {
                    return SdHandler;

                    void SdHandler(object sender, SerialDataReceivedEventArgs args)
                    {
                        handler(args);
                    }
                },
                handler => _serialPort.DataReceived += handler,
                handler => _serialPort.DataReceived -= handler)
            .Select(args =>
            {
                var data = ReadAllBytes().ToArray();

                return data;
            });
    }

    private IEnumerable<byte> ReadAllBytes()
    {
        var buffer = new byte[_serialPort.BytesToRead];

        var bytesRead = _serialPort.Read(buffer, 0, buffer.Length);

        return buffer.Take(bytesRead);
    }

    public void Open()
    {
        _serialPort.Open();
    }

    public void Close()
    {
        _serialPort.Close();
    }

    public void Dispose()
    {
        _serialPort.Dispose();
    }

    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return _dataObservable.Subscribe(observer);
    }
}

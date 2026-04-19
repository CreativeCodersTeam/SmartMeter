namespace CreativeCoders.SmartMeter.Core;

/// <summary>
/// Default <see cref="IReactiveSerialPortFactory"/> that creates real <see cref="ReactiveSerialPort"/>
/// instances backed by <see cref="System.IO.Ports.SerialPort"/>.
/// </summary>
public sealed class ReactiveSerialPortFactory : IReactiveSerialPortFactory
{
    public IReactiveSerialPort Create(string portName) => new ReactiveSerialPort(portName);
}

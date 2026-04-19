namespace CreativeCoders.SmartMeter.Core;

/// <summary>
/// Factory abstraction that builds an <see cref="IReactiveSerialPort"/> for a given port name.
/// Used so services that need a dedicated port (unlocker, data producer) can be tested with
/// fakes instead of real <see cref="System.IO.Ports.SerialPort"/> instances.
/// </summary>
public interface IReactiveSerialPortFactory
{
    /// <summary>Creates a new serial port wrapper for the specified OS-level port name.</summary>
    IReactiveSerialPort Create(string portName);
}

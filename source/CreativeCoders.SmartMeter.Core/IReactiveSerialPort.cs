namespace CreativeCoders.SmartMeter.Core;

/// <summary>
/// Observable byte stream of an opened serial port. Abstracts <see cref="ReactiveSerialPort"/>
/// so that callers can be unit-tested without a real hardware port.
/// </summary>
public interface IReactiveSerialPort : IObservable<byte[]>, IDisposable
{
    /// <summary>True when the underlying port is open and ready for I/O.</summary>
    bool IsOpen { get; }

    /// <summary>Opens the underlying serial port.</summary>
    void Open();

    /// <summary>Closes the underlying serial port without disposing it.</summary>
    void Close();

    /// <summary>Writes the given bytes to the serial port.</summary>
    void Write(byte[] data);
}

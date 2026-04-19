using System.Reactive.Subjects;

namespace CreativeCoders.SmartMeter.Core.Tests.Fixtures;

/// <summary>
/// Hand-rolled test double for <see cref="IReactiveSerialPort"/> that records writes
/// and allows tests to push raw bytes to subscribed observers. Using a plain class
/// keeps observable semantics predictable and avoids the ref-struct limitations of
/// FakeItEasy when verifying observer interactions.
/// </summary>
internal sealed class FakeReactiveSerialPort : IReactiveSerialPort
{
    private readonly Subject<byte[]> _subject = new();

    public List<byte[]> Writes { get; } = [];

    public int OpenCount { get; private set; }

    public int CloseCount { get; private set; }

    public int DisposeCount { get; private set; }

    public bool IsOpen { get; private set; }

    public Func<byte[], Exception?>? WriteBehavior { get; set; }

    public void Open()
    {
        OpenCount++;
        IsOpen = true;
    }

    public void Close()
    {
        CloseCount++;
        IsOpen = false;
    }

    public void Write(byte[] data)
    {
        var error = WriteBehavior?.Invoke(data);

        if (error is not null)
        {
            throw error;
        }

        Writes.Add(data);
    }

    public IDisposable Subscribe(IObserver<byte[]> observer) => _subject.Subscribe(observer);

    public void PushBytes(byte[] data) => _subject.OnNext(data);

    public void Dispose()
    {
        DisposeCount++;
        _subject.Dispose();
    }
}

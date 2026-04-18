namespace CreativeCoders.SmartMessageLanguage.Framing;

public interface ISmlMessageDetector : IDisposable
{
    /// <summary>Raised once for every complete frame detected in the stream.</summary>
    event EventHandler<SmlMessageEventArgs>? MessageReceived;

    /// <summary>Observable stream of detected frames; fires in parallel with <see cref="SmlMessageDetector.MessageReceived"/>.</summary>
    IObservable<SmlFrame> Messages { get; }

    /// <summary>Appends a chunk of bytes from the transport and extracts any newly completed frames.</summary>
    /// <param name="data">Bytes received from the underlying stream.</param>
    void Append(ReadOnlySpan<byte> data);

    /// <summary>Clears any partial buffered data and resets the internal state.</summary>
    void Reset();
}

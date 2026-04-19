using System.Reactive.Subjects;
using CreativeCoders.Core;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMessageLanguage.Framing;

/// <summary>
/// Streaming detector for SML transport v1 frames.
/// </summary>
/// <remarks>
/// Callers feed raw bytes via <see cref="Append"/> as they arrive from a serial line,
/// TCP socket or similar source. The detector locates the start escape sequence
/// (<c>1B1B1B1B 01010101</c>), tracks the end escape (<c>1B1B1B1B 1A &lt;pad&gt; &lt;crc-lo&gt; &lt;crc-hi&gt;</c>)
/// while correctly handling doubled <c>0x1B</c> runs inside the body, validates the
/// CRC-16/X-25 and raises both a classic <see cref="MessageReceived"/> event and the
/// <see cref="Messages"/> observable for every complete frame. Frames with an invalid
/// CRC are still emitted with <see cref="SmlFrame.IsCrcValid"/> set to <c>false</c>.
/// </remarks>
public sealed class SmlMessageDetector : ISmlMessageDetector
{
    // Defensive cap to prevent unbounded buffer growth if the peer never closes a frame.
    private const int MaxBufferSize = 64 * 1024;

    private static readonly byte[] StartEscape =
        [0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01, 0x01, 0x01];

    private readonly Subject<SmlFrame> _subject = new Subject<SmlFrame>();
    private readonly ILogger<SmlMessageDetector> _logger;

    private byte[] _buffer = [];
    private int _length;
    private bool _startFound;

    /// <summary>Creates a new detector and routes diagnostic events to <paramref name="logger"/>.</summary>
    /// <param name="logger">Logger used for streaming/diagnostic events; pass
    /// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}.Instance"/> to silence logging.</param>
    public SmlMessageDetector(ILogger<SmlMessageDetector> logger)
    {
        _logger = Ensure.NotNull(logger);
    }

    /// <summary>Raised once for every complete frame detected in the stream.</summary>
    public event EventHandler<SmlMessageEventArgs>? MessageReceived;

    /// <summary>Observable stream of detected frames; fires in parallel with <see cref="MessageReceived"/>.</summary>
    public IObservable<SmlFrame> Messages => _subject;

    /// <summary>Appends a chunk of bytes from the transport and extracts any newly completed frames.</summary>
    /// <param name="data">Bytes received from the underlying stream.</param>
    public void Append(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        AppendToBuffer(data);
        SmlMessageDetectorLog.BytesAppended(_logger, data.Length, _length);

        while (TryExtractFrame(out var frame))
        {
            if (!frame.IsCrcValid)
            {
                SmlMessageDetectorLog.InvalidCrc(_logger, frame.MessageBytes.Length);
            }

            SmlMessageDetectorLog.FrameDetected(_logger, frame.MessageBytes.Length,
                frame.PayloadBytes.Length, frame.IsCrcValid);

            MessageReceived?.Invoke(this, new SmlMessageEventArgs(frame));
            _subject.OnNext(frame);
        }

        // If the buffer grows unbounded without a valid frame, discard to avoid DoS.
        if (_length > MaxBufferSize)
        {
            SmlMessageDetectorLog.BufferOverflow(_logger, MaxBufferSize);
            Reset();
        }
    }

    /// <summary>Clears any partial buffered data and resets the internal state.</summary>
    public void Reset()
    {
        _length = 0;
        _startFound = false;
        SmlMessageDetectorLog.DetectorReset(_logger);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        _subject.OnCompleted();
        _subject.Dispose();
    }

    private void AppendToBuffer(ReadOnlySpan<byte> data)
    {
        var required = _length + data.Length;

        if (required > _buffer.Length)
        {
            var newSize = Math.Max(required, Math.Max(_buffer.Length * 2, 256));
            Array.Resize(ref _buffer, newSize);
        }

        data.CopyTo(_buffer.AsSpan(_length));
        _length += data.Length;
    }

    private void Consume(int count)
    {
        var remaining = _length - count;

        if (remaining > 0)
        {
            Buffer.BlockCopy(_buffer, count, _buffer, 0, remaining);
        }

        _length = remaining;
    }

    private bool TryExtractFrame(out SmlFrame frame)
    {
        frame = null!;

        if (!_startFound && !TryAnchorOnStart())
        {
            return false;
        }

        // Scan from just after the 8-byte start escape for the end escape, while
        // correctly stepping over doubled 0x1B escape runs inside the body.
        var pos = StartEscape.Length;

        while (pos + 4 <= _length)
        {
            if (!IsEscapeMark(_buffer, pos))
            {
                pos++;
                continue;
            }

            // Need 4 more bytes to disambiguate between escaped run and end marker.
            if (pos + 8 > _length)
            {
                return false;
            }

            if (IsEscapeMark(_buffer, pos + 4))
            {
                // Doubled 0x1B run: payload literal, skip past all eight bytes.
                pos += 8;
                continue;
            }

            if (_buffer[pos + 4] == 0x1A)
            {
                var frameLength = pos + 8;
                frame = BuildFrame(frameLength);
                Consume(frameLength);
                _startFound = false;

                return true;
            }

            // Malformed escape sequence (e.g. stray 1B1B1B1B followed by junk).
            // Discard the start escape and resync on the next candidate.
            SmlMessageDetectorLog.MalformedEscape(_logger, pos);
            Consume(1);
            _startFound = false;

            return TryExtractFrame(out frame);
        }

        return false;
    }

    private bool TryAnchorOnStart()
    {
        var idx = IndexOf(_buffer.AsSpan(0, _length), StartEscape);

        if (idx < 0)
        {
            // Keep only the last (startEscape.Length - 1) bytes so a start escape
            // spanning two Append calls can still be detected.
            var keep = Math.Min(StartEscape.Length - 1, _length);
            var drop = _length - keep;

            if (drop > 0)
            {
                Consume(drop);
            }

            return false;
        }

        if (idx > 0)
        {
            Consume(idx);
        }

        _startFound = true;
        SmlMessageDetectorLog.AnchoredOnStart(_logger, idx);

        return true;
    }

    private SmlFrame BuildFrame(int frameLength)
    {
        // Raw bytes as transmitted (incl. start/end escape and any escape doubling).
        var messageBytes = _buffer.AsSpan(0, frameLength).ToArray();

        var paddingBytes = messageBytes[frameLength - 3];
        var storedCrc = (ushort)(messageBytes[frameLength - 2] | (messageBytes[frameLength - 1] << 8));
        var computedCrc = Crc16X25.Compute(messageBytes.AsSpan(0, frameLength - 2));
        var isCrcValid = storedCrc == computedCrc;

        var payload = ExtractPayload(messageBytes, paddingBytes);

        return new SmlFrame(messageBytes, payload, isCrcValid, paddingBytes);
    }

    private static byte[] ExtractPayload(byte[] frame, int paddingBytes)
    {
        // Body lies between the 8-byte start escape and the 8-byte end escape,
        // minus any trailing 0x00 padding bytes used to align to a 4-byte boundary.
        var bodyStart = StartEscape.Length;
        var bodyEnd = frame.Length - 8 - paddingBytes;

        if (bodyEnd < bodyStart)
        {
            return [];
        }

        var body = frame.AsSpan(bodyStart, bodyEnd - bodyStart);

        // De-escape: any 8-byte run of 0x1B was originally 4 bytes of 0x1B in the payload.
        var output = new byte[body.Length];
        var written = 0;
        var i = 0;

        while (i < body.Length)
        {
            if (i + 8 <= body.Length
                && IsEscapeMark(body, i)
                && IsEscapeMark(body, i + 4))
            {
                output[written++] = 0x1B;
                output[written++] = 0x1B;
                output[written++] = 0x1B;
                output[written++] = 0x1B;
                i += 8;
            }
            else
            {
                output[written++] = body[i++];
            }
        }

        if (written == output.Length)
        {
            return output;
        }

        var trimmed = new byte[written];
        Array.Copy(output, trimmed, written);

        return trimmed;
    }

    private static bool IsEscapeMark(ReadOnlySpan<byte> data, int offset) =>
        data[offset] == 0x1B
        && data[offset + 1] == 0x1B
        && data[offset + 2] == 0x1B
        && data[offset + 3] == 0x1B;

    private static bool IsEscapeMark(byte[] data, int offset) =>
        IsEscapeMark(data.AsSpan(), offset);

    private static int IndexOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length)
        {
            return -1;
        }

        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.Slice(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }
}

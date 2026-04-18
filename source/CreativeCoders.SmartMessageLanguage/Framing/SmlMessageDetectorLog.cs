using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMessageLanguage.Framing;

// Source-generated, allocation-free logging helpers for SmlMessageDetector.
// Event IDs 1000-1099 are reserved for the framing layer.
internal static partial class SmlMessageDetectorLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug,
        Message = "Appended {ByteCount} bytes to SML buffer (buffered={BufferedBytes})")]
    public static partial void BytesAppended(ILogger logger, int byteCount, int bufferedBytes);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug,
        Message = "Anchored on SML start escape at buffer offset {Offset}")]
    public static partial void AnchoredOnStart(ILogger logger, int offset);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information,
        Message = "Detected SML frame: frameLength={FrameLength}, payloadLength={PayloadLength}, crcValid={CrcValid}")]
    public static partial void FrameDetected(ILogger logger, int frameLength, int payloadLength, bool crcValid);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Warning,
        Message = "SML frame failed CRC check (frameLength={FrameLength})")]
    public static partial void InvalidCrc(ILogger logger, int frameLength);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning,
        Message = "Malformed SML escape sequence at buffer offset {Offset}; resyncing")]
    public static partial void MalformedEscape(ILogger logger, int offset);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Warning,
        Message = "SML buffer exceeded {MaxBufferSize} bytes without a complete frame; discarding buffer")]
    public static partial void BufferOverflow(ILogger logger, int maxBufferSize);

    [LoggerMessage(EventId = 1020, Level = LogLevel.Debug, Message = "SML detector reset")]
    public static partial void DetectorReset(ILogger logger);
}

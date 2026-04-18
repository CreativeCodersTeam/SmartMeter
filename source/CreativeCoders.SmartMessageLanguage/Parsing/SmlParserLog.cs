using CreativeCoders.SmartMessageLanguage.Tlv;
using CreativeCoders.SmartMessageLanguage.Units;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMessageLanguage.Parsing;

// Source-generated, allocation-free logging helpers for SmlParser.
// Event IDs 2000-2099 are reserved for the parsing layer.
internal static partial class SmlParserLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Debug,
        Message = "Starting SML parse for payload of {PayloadLength} bytes")]
    public static partial void ParseStarted(ILogger logger, int payloadLength);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Debug,
        Message = "Found SML_GetList.Res with {EntryCount} entries")]
    public static partial void GetListResponseFound(ILogger logger, int entryCount);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Debug,
        Message = "Parsed OBIS value {ObisCode} = {Value} {Unit} (scaler={Scaler})")]
    public static partial void ObisValueParsed(ILogger logger, string obisCode, decimal? value,
        SmlUnit unit, sbyte scaler);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Debug,
        Message = "SML parse completed: {ValueCount} OBIS values, {WarningCount} warnings")]
    public static partial void ParseCompleted(ILogger logger, int valueCount, int warningCount);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Warning,
        Message = "Unknown SML unit code {UnitCode} for OBIS {ObisCode}")]
    public static partial void UnknownUnit(ILogger logger, byte unitCode, string obisCode);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Warning,
        Message = "Unexpected SML TLV type {TlvType} ({Context})")]
    public static partial void UnexpectedTlvType(ILogger logger, SmlValueType tlvType, string context);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Warning,
        Message = "Unsupported SML value type {TlvType} for OBIS {ObisCode}")]
    public static partial void UnsupportedValueType(ILogger logger, SmlValueType tlvType, string obisCode);

    [LoggerMessage(EventId = 2020, Level = LogLevel.Error,
        Message = "Malformed SML envelope: {Reason}")]
    public static partial void EnvelopeError(ILogger logger, string reason);
}

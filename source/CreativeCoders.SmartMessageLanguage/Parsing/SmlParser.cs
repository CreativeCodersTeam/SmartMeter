using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CreativeCoders.Core;
using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Tlv;
using CreativeCoders.SmartMessageLanguage.Units;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMessageLanguage.Parsing;

/// <summary>
/// High-level parser that extracts OBIS values from an SML frame.
/// </summary>
/// <remarks>
/// The parser walks the top-level <c>SML_File</c> / <c>SML_Message</c> structure,
/// locates every <c>SML_GetList.Res</c> body (message type <c>0x00000701</c>) and
/// turns each entry of the <c>valList</c> into an <see cref="ObisValue"/>. Other
/// message body types are skipped. Unknown units or unexpected value types are
/// reported via <see cref="SmlParseResult.Warnings"/>; the parser never throws on
/// well-formed but semantically unusual input.
/// </remarks>
public sealed class SmlParser : ISmlParser
{
    private const uint GetListResponseId = 0x00000701;

    private readonly ILogger<SmlParser> _logger;

    /// <summary>Creates a parser and routes diagnostic events to <paramref name="logger"/>.</summary>
    /// <param name="logger">Logger used for diagnostic events; pass
    /// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}.Instance"/> to silence logging.</param>
    public SmlParser(ILogger<SmlParser> logger)
    {
        _logger = Ensure.NotNull(logger);
    }

    /// <summary>Parses all OBIS values contained in the given frame.</summary>
    /// <param name="frame">Frame produced by <see cref="SmlMessageDetector"/>.</param>
    public SmlParseResult Parse(SmlFrame frame)
    {
        Ensure.NotNull(frame);

        return Parse(frame.PayloadBytes);
    }

    /// <summary>Parses all OBIS values contained in the given de-escaped payload.</summary>
    /// <param name="payload">Payload bytes of an SML frame (without start/end escape).</param>
    public SmlParseResult Parse(ReadOnlySpan<byte> payload)
    {
        SmlParserLog.ParseStarted(_logger, payload.Length);

        var values = new List<ObisValue>();
        var warnings = new List<string>();
        var reader = new SmlTlvReader(payload);

        while (reader.Read())
        {
            var element = reader.Current;

            if (element.IsEndOfMessage)
            {
                continue;
            }

            if (element.Type != SmlMessageValueType.List)
            {
                // Top level must be a sequence of messages (lists); anything else is stray.
                var message = $"Unexpected top-level TLV type {element.Type}";
                warnings.Add(message);
                SmlParserLog.EnvelopeError(_logger, message);

                continue;
            }

            ProcessMessage(ref reader, element.ListLength, values, warnings);
        }

        SmlParserLog.ParseCompleted(_logger, values.Count, warnings.Count);

        return new SmlParseResult(values, warnings);
    }

    private void ProcessMessage(ref SmlTlvReader reader, int entryCount,
        List<ObisValue> values, List<string> warnings)
    {
        // SML_Message = List of 6: transactionId, groupNo, abortOnError, messageBody, crc16, endOfSmlMsg
        // messageBody = List of 2: messageBodyType (Unsigned), messageBodyChoice (type-specific List)
        if (entryCount != 6)
        {
            SkipListEntries(ref reader, entryCount);

            return;
        }

        // Fields 1-3 are header values we don't care about; skip each.
        for (var i = 0; i < 3; i++)
        {
            if (!reader.Read())
            {
                return;
            }

            reader.SkipCurrent();
        }

        // Field 4: messageBody list.
        if (!reader.Read() || reader.Current.Type != SmlMessageValueType.List || reader.Current.ListLength != 2)
        {
            warnings.Add("Malformed SML_Message body wrapper");
            SkipListEntries(ref reader, 2);

            return;
        }

        if (!reader.Read() || reader.Current.Type != SmlMessageValueType.Unsigned)
        {
            warnings.Add("Missing messageBody type tag");
            SkipListEntries(ref reader, 3);

            return;
        }

        var messageBodyType = (uint)reader.Current.GetUInt64();

        if (!reader.Read())
        {
            return;
        }

        if (messageBodyType == GetListResponseId && reader.Current.Type == SmlMessageValueType.List)
        {
            SmlParserLog.GetListResponseFound(_logger, reader.Current.ListLength);
            ProcessGetListResponse(ref reader, reader.Current.ListLength, values, warnings);
        }
        else
        {
            reader.SkipCurrent();
        }

        // Fields 5 and 6: crc16 + endOfSmlMsg.
        for (var i = 0; i < 2; i++)
        {
            if (!reader.Read())
            {
                return;
            }

            reader.SkipCurrent();
        }
    }

    private void ProcessGetListResponse(ref SmlTlvReader reader, int entryCount,
        List<ObisValue> values, List<string> warnings)
    {
        // SML_GetList.Res = List of 7: clientId, serverId, listName, actSensorTime,
        //                              valList, listSignature, actGatewayTime.
        if (entryCount != 7)
        {
            warnings.Add($"GetListResponse with unexpected field count {entryCount}");
            SkipListEntries(ref reader, entryCount);

            return;
        }

        // Skip first 4 fields.
        for (var i = 0; i < 4; i++)
        {
            if (!reader.Read())
            {
                return;
            }

            reader.SkipCurrent();
        }

        // Field 5: valList (or OPTIONAL null). When present it is a List.
        if (!reader.Read())
        {
            return;
        }

        if (reader.Current.Type == SmlMessageValueType.List)
        {
            var listCount = reader.Current.ListLength;

            for (var i = 0; i < listCount; i++)
            {
                ReadValListEntry(ref reader, values, warnings);
            }
        }
        else
        {
            // valList is OPTIONAL; a missing list is encoded as an empty octet string.
            // Any other type is surprising.
            if (reader.Current.Type != SmlMessageValueType.OctetString)
            {
                warnings.Add($"Unexpected valList type {reader.Current.Type}");
                SmlParserLog.UnexpectedTlvType(_logger, reader.Current.Type, "valList");
            }
        }

        // Skip remaining fields 6 & 7.
        for (var i = 0; i < 2; i++)
        {
            if (!reader.Read())
            {
                return;
            }

            reader.SkipCurrent();
        }
    }

    [SuppressMessage("csharpsquid", "S3776",
        Justification = "This method is long but straightforward; refactor if it grows more complex.")]
    private void ReadValListEntry(ref SmlTlvReader reader, List<ObisValue> values,
        List<string> warnings)
    {
        // SML_ListEntry = List of 7: objName (OctetString), status, valTime,
        //                            unit (Unsigned8), scaler (Integer8), value, valueSignature.
        if (!reader.Read() || reader.Current.Type != SmlMessageValueType.List)
        {
            warnings.Add("valList entry is not a list");

            return;
        }

        var entryCount = reader.Current.ListLength;

        if (entryCount < 6)
        {
            warnings.Add($"valList entry too short ({entryCount} fields)");
            SkipListEntries(ref reader, entryCount);

            return;
        }

        // objName
        if (!reader.Read() || reader.Current.Type != SmlMessageValueType.OctetString)
        {
            warnings.Add("valList entry missing objName");
            SkipListEntries(ref reader, entryCount - 1);

            return;
        }

        var obisCode = FormatObisCode(reader.Current.Raw);

        // status + valTime: skip.
        for (var i = 0; i < 2; i++)
        {
            if (!reader.Read())
            {
                return;
            }

            reader.SkipCurrent();
        }

        // unit
        if (!reader.Read())
        {
            return;
        }

        var unit = SmlUnit.Unknown;

        if (reader.Current.Type == SmlMessageValueType.Unsigned)
        {
            var unitCode = (byte)reader.Current.GetUInt64();
            unit = Enum.IsDefined((SmlUnit)unitCode) ? (SmlUnit)unitCode : SmlUnit.Unknown;

            if (unit == SmlUnit.Unknown && unitCode != 0)
            {
                warnings.Add($"Unknown unit code {unitCode} for {obisCode}");
                SmlParserLog.UnknownUnit(_logger, unitCode, obisCode);
            }
        }

        // scaler
        if (!reader.Read())
        {
            return;
        }

        sbyte scaler = 0;

        if (reader.Current.Type == SmlMessageValueType.Integer)
        {
            scaler = (sbyte)reader.Current.GetInt64();
        }

        // value
        if (!reader.Read())
        {
            return;
        }

        var rawType = reader.Current.Type;
        var rawBytes = reader.Current.GetOctetString();
        var decimalValue = ComputeDecimalValue(reader.Current, scaler, warnings, obisCode);

        values.Add(new ObisValue(obisCode, decimalValue, unit, scaler, rawBytes, rawType));
        SmlParserLog.ObisValueParsed(_logger, obisCode, decimalValue, unit, scaler);

        // Remaining fields (valueSignature and any extras).
        for (var i = 6; i < entryCount; i++)
        {
            if (!reader.Read())
            {
                return;
            }

            reader.SkipCurrent();
        }
    }

    private decimal? ComputeDecimalValue(SmlTlvElement value, sbyte scaler,
        List<string> warnings, string obisCode)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (value.Type)
        {
            case SmlMessageValueType.Unsigned:
                return ApplyScaler(value.GetUInt64(), scaler);
            case SmlMessageValueType.Integer:
                return ApplyScaler(value.GetInt64(), scaler);
            case SmlMessageValueType.Boolean:
                return value.GetBool() ? 1m : 0m;
            case SmlMessageValueType.OctetString:
                // Non-numeric (e.g. server ID as octet string); caller can read RawValue.
                return null;
            default:
                warnings.Add($"Unsupported value type {value.Type} for {obisCode}");
                SmlParserLog.UnsupportedValueType(_logger, value.Type, obisCode);

                return null;
        }
    }

    private static decimal ApplyScaler(long raw, sbyte scaler) =>
        scaler == 0 ? raw : raw * (decimal)Math.Pow(10, scaler);

    private static decimal ApplyScaler(ulong raw, sbyte scaler) =>
        scaler == 0 ? raw : raw * (decimal)Math.Pow(10, scaler);

    private static string FormatObisCode(ReadOnlySpan<byte> raw)
    {
        // Standard OBIS identifier is 6 bytes: A-B:C.D.E*F. Some meters omit F; default to 255.
        if (raw.Length < 5)
        {
            return ToHex(raw);
        }

        var a = raw[0];
        var b = raw[1];
        var c = raw[2];
        var d = raw[3];
        var e = raw[4];
        var f = raw.Length >= 6 ? raw[5] : (byte)255;

        return string.Create(CultureInfo.InvariantCulture,
            $"{a}-{b}:{c}.{d}.{e}*{f}");
    }

    private static string ToHex(ReadOnlySpan<byte> data)
    {
        return data.IsEmpty
            ? string.Empty
            : Convert.ToHexString(data);
    }

    private static void SkipListEntries(ref SmlTlvReader reader, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (!reader.Read())
            {
                return;
            }

            reader.SkipCurrent();
        }
    }
}

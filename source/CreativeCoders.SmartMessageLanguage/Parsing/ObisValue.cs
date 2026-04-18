using CreativeCoders.SmartMessageLanguage.Tlv;
using CreativeCoders.SmartMessageLanguage.Units;

namespace CreativeCoders.SmartMessageLanguage.Parsing;

/// <summary>
/// Represents a single OBIS value extracted from an SML <c>GetListResponse</c>.
/// </summary>
/// <param name="ObisCode">OBIS code formatted as <c>A-B:C.D.E*F</c>.</param>
/// <param name="Value">Scaled numeric value if the raw value is numeric, otherwise <c>null</c>.</param>
/// <param name="Unit">Unit enum; <see cref="SmlUnit.Unknown"/> when absent or unrecognised.</param>
/// <param name="Scaler">Decimal scaler applied to produce <paramref name="Value"/> (<c>Value = raw * 10^Scaler</c>).</param>
/// <param name="RawValue">Raw bytes of the value element as sent on the wire.</param>
/// <param name="RawType">TLV primitive type of the raw value.</param>
public sealed record ObisValue(
    string ObisCode,
    decimal? Value,
    SmlUnit Unit,
    sbyte Scaler,
    byte[] RawValue,
    SmlMessageValueType RawType);

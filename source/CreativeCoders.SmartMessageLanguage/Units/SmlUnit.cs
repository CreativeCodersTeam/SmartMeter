namespace CreativeCoders.SmartMessageLanguage.Units;

/// <summary>
/// Subset of DLMS/IEC 62056-62 unit codes commonly used by electricity meters.
/// The numeric values match the SML <c>SML_Unit</c> codes as received on the wire.
/// </summary>
public enum SmlUnit : byte
{
    /// <summary>Unknown or unassigned unit.</summary>
    Unknown = 0,

    /// <summary>Year (<c>a</c>).</summary>
    Year = 1,

    /// <summary>Month (<c>mo</c>).</summary>
    Month = 2,

    /// <summary>Week (<c>wk</c>).</summary>
    Week = 3,

    /// <summary>Day (<c>d</c>).</summary>
    Day = 4,

    /// <summary>Hour (<c>h</c>).</summary>
    Hour = 5,

    /// <summary>Minute (<c>min</c>).</summary>
    Minute = 6,

    /// <summary>Second (<c>s</c>).</summary>
    Second = 7,

    /// <summary>Degree (<c>°</c>, phase angle).</summary>
    Degree = 8,

    /// <summary>Degree Celsius (<c>°C</c>).</summary>
    DegreeCelsius = 9,

    /// <summary>Metre (<c>m</c>).</summary>
    Metre = 11,

    /// <summary>Metre per second (<c>m/s</c>).</summary>
    MetrePerSecond = 12,

    /// <summary>Cubic metre (<c>m³</c>).</summary>
    CubicMetre = 13,

    /// <summary>Kilogram (<c>kg</c>).</summary>
    Kilogram = 17,

    /// <summary>Newton (<c>N</c>).</summary>
    Newton = 19,

    /// <summary>Pascal (<c>Pa</c>).</summary>
    Pascal = 22,

    /// <summary>Watt (<c>W</c>, active power).</summary>
    Watt = 27,

    /// <summary>Volt-ampere (<c>VA</c>, apparent power).</summary>
    VoltAmpere = 28,

    /// <summary>Volt-ampere reactive (<c>var</c>, reactive power).</summary>
    Var = 29,

    /// <summary>Watt-hour (<c>Wh</c>, active energy).</summary>
    WattHour = 30,

    /// <summary>Volt-ampere-hour (<c>VAh</c>, apparent energy).</summary>
    VoltAmpereHour = 31,

    /// <summary>Volt-ampere reactive hour (<c>varh</c>, reactive energy).</summary>
    VarHour = 32,

    /// <summary>Ampere (<c>A</c>, current).</summary>
    Ampere = 33,

    /// <summary>Coulomb (<c>C</c>, charge).</summary>
    Coulomb = 34,

    /// <summary>Volt (<c>V</c>, voltage).</summary>
    Volt = 35,

    /// <summary>Volt per metre (<c>V/m</c>).</summary>
    VoltPerMetre = 36,

    /// <summary>Farad (<c>F</c>).</summary>
    Farad = 37,

    /// <summary>Ohm (<c>Ω</c>).</summary>
    Ohm = 38,

    /// <summary>Power factor (<c>cos φ</c>, dimensionless).</summary>
    PowerFactor = 43,

    /// <summary>Hertz (<c>Hz</c>, frequency).</summary>
    Hertz = 44,

    /// <summary>Percent (<c>%</c>).</summary>
    Percent = 56,

    /// <summary>Ampere-hour (<c>Ah</c>).</summary>
    AmpereHour = 57,

    /// <summary>Count (dimensionless).</summary>
    Count = 255
}

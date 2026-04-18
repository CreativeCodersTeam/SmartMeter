namespace CreativeCoders.SmartMeter.Server.Core.Unlock;

/// <summary>
/// Defines how the PIN is transmitted over the optical interface of a smart meter.
/// Different vendors require different wire formats.
/// </summary>
public enum SmartMeterPinStrategy
{
    /// <summary>
    /// PIN is sent as a single ASCII block terminated by a configurable line ending
    /// (typical for EMH / eHZ meters).
    /// </summary>
    EmhAsciiBlock,

    /// <summary>
    /// PIN digits are sent one-by-one with a configurable delay between them
    /// (typical for Easymeter Q3A/Q3B/Q3D).
    /// </summary>
    EasymeterDigitByDigit,

    /// <summary>
    /// PIN is sent as a single ASCII block; a 0x06 ACK byte is treated as an
    /// immediate success indicator (typical for some ISKRA MT-series meters).
    /// </summary>
    IskraAsciiBlock
}

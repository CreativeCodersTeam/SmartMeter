namespace CreativeCoders.SmartMeter.Core.Unlock;

/// <summary>
/// Options controlling the PIN unlock procedure for the smart meter's optical
/// interface. Defaults target a typical EMH / eHZ meter.
/// </summary>
public sealed record SmartMeterUnlockOptions
{
    /// <summary>Wire-format strategy used to transmit the PIN.</summary>
    public SmartMeterPinStrategy Strategy { get; init; } = SmartMeterPinStrategy.EmhAsciiBlock;

    /// <summary>Line ending appended after an ASCII-block PIN (EMH / ISKRA strategies).</summary>
    public string LineEnding { get; init; } = "\r\n";

    /// <summary>Delay between individual digits when using <see cref="SmartMeterPinStrategy.EasymeterDigitByDigit"/>.</summary>
    public TimeSpan DigitDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Wait time after opening the port before sending the PIN.</summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Maximum time to wait for verification evidence (extended OBIS codes / ACK) after the PIN has been sent.</summary>
    public TimeSpan VerificationTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// When <c>true</c>, the data stream is observed after sending the PIN and the
    /// result reflects whether verification evidence was found. When <c>false</c>,
    /// the method returns immediately after writing with <see cref="SmartMeterUnlockOutcome.VerificationSkipped"/>.
    /// </summary>
    public bool Verify { get; init; } = true;

    /// <summary>
    /// OBIS codes (format "A-B:C.D.E*F") whose appearance in the raw byte stream is
    /// interpreted as a successful unlock. Defaults cover instantaneous power, sum
    /// active power, voltages and per-phase powers.
    /// </summary>
    public IReadOnlyList<string> ExpectedObisCodes { get; init; } =
    [
        "1-0:1.7.0*255",
        "1-0:16.7.0*255",
        "1-0:21.7.0*255",
        "1-0:41.7.0*255",
        "1-0:61.7.0*255",
        "1-0:32.7.0*255",
        "1-0:52.7.0*255",
        "1-0:72.7.0*255"
    ];
}

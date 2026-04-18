namespace CreativeCoders.SmartMeter.Core.Unlock;

/// <summary>
/// Structured result of a PIN unlock attempt on the smart meter.
/// </summary>
/// <param name="Success">True if the PIN is considered to have been accepted.</param>
/// <param name="Outcome">Categorised outcome of the attempt.</param>
/// <param name="DetectedObisCodes">Extended OBIS codes detected on the stream after sending the PIN.</param>
/// <param name="Elapsed">Wall-clock time spent in <c>UnlockAsync</c> from start to result.</param>
/// <param name="Message">Optional human-readable detail (e.g. exception message).</param>
public sealed record SmartMeterUnlockResult(
    bool Success,
    SmartMeterUnlockOutcome Outcome,
    IReadOnlyList<string> DetectedObisCodes,
    TimeSpan Elapsed,
    string? Message = null);

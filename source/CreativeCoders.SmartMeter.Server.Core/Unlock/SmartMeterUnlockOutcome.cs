namespace CreativeCoders.SmartMeter.Server.Core.Unlock;

/// <summary>
/// Result category of a PIN unlock attempt.
/// </summary>
public enum SmartMeterUnlockOutcome
{
    /// <summary>The meter emitted data that indicates a successful unlock.</summary>
    PinAccepted,

    /// <summary>Verification was skipped by configuration.</summary>
    VerificationSkipped,

    /// <summary>PIN was sent but no extended data / ACK arrived within the timeout.</summary>
    VerificationTimeout,

    /// <summary>Sending the PIN over the serial port failed.</summary>
    WriteFailed,

    /// <summary>The operation was cancelled by the caller.</summary>
    Cancelled
}

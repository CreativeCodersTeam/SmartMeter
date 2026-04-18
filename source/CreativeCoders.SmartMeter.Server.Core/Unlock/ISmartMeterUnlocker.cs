namespace CreativeCoders.SmartMeter.Server.Core.Unlock;

public interface ISmartMeterUnlocker : IDisposable
{
    /// <summary>
    /// Sends the given PIN to the smart meter via the optical coupler connected
    /// to the serial port in order to unlock the extended data set (instantaneous
    /// power, per-phase values, voltages, ...). Optionally verifies the unlock by
    /// observing the incoming byte stream for extended OBIS codes.
    /// </summary>
    /// <param name="pin">PIN as printable ASCII digits. Must not be empty.</param>
    /// <param name="options">Transport and verification options. Defaults target EMH / eHZ meters.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A structured <see cref="SmartMeterUnlockResult"/> describing the outcome.</returns>
    Task<SmartMeterUnlockResult> UnlockAsync(
        string pin,
        SmartMeterUnlockOptions? options = null,
        CancellationToken cancellationToken = default);
}

using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using CreativeCoders.Core;
using CreativeCoders.SmartMeter.DataProcessing;
using CreativeCoders.SmartMeter.Server.Core.Unlock;
using CreativeCoders.SmartMeter.Sml.Reactive;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMeter.Server.Core.SmlData;

public sealed class SmartMeterDataProducer(
    ISmartMeterReactiveDataPipeline reactiveDataPipeline,
    ILogger<SmartMeterDataProducer> logger) : ISmartMeterDataProducer
{
    private readonly ISmartMeterReactiveDataPipeline _reactiveDataPipeline = Ensure.NotNull(reactiveDataPipeline);
    private readonly ILogger<SmartMeterDataProducer> _logger = Ensure.NotNull(logger);
    private readonly ReactiveSerialPort _serialPort = new ReactiveSerialPort("/dev/ttyUSB0");

    private IDisposable? _subscription;

    public Task StartAsync(IObserver<SmartMeterValue> observer)
    {
        _logger.LogInformation("Starting SmartMeter data producer");

        _reactiveDataPipeline
            .SubscribeOn(new TaskPoolScheduler(new TaskFactory()))
            .Subscribe(observer);

        _subscription ??= _serialPort
            .Subscribe(_reactiveDataPipeline);

        _logger.LogInformation("SmartMeter data producer initialized");

        OpenSerialPort();

        return Task.CompletedTask;
    }

    private void OpenSerialPort()
    {
        _logger.LogInformation("Opening serial port...");
        _serialPort.Open();
        _logger.LogInformation("Serial port opened");
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Stopping SmartMeter data producer");

        DisposingSubscription();

        CloseSerialPort();

        _logger.LogInformation("SmartMeter data producer stopped");

        return Task.CompletedTask;
    }

    public async Task<SmartMeterUnlockResult> UnlockAsync(
        string pin,
        SmartMeterUnlockOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(pin);

        options ??= new SmartMeterUnlockOptions();

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Unlocking smart meter via {Strategy}, pinLength={PinLength}, verify={Verify}, verificationTimeout={Timeout}",
            options.Strategy, pin.Length, options.Verify, options.VerificationTimeout);

        // Ensure the port is open so we can write and observe responses. Don't close
        // it here, so the caller can continue using the same producer afterwards.
        if (!_serialPort.IsOpen)
        {
            _logger.LogInformation("Serial port is closed, opening it for unlock procedure...");
            _serialPort.Open();
            _logger.LogInformation("Serial port opened");
        }

        try
        {
            if (options.InitialDelay > TimeSpan.Zero)
            {
                _logger.LogDebug("Waiting initial delay {Delay} before sending PIN", options.InitialDelay);

                await Task.Delay(options.InitialDelay, cancellationToken).ConfigureAwait(false);
            }

            var detected = new HashSet<string>(StringComparer.Ordinal);
            var verificationTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var expectAck = options.Strategy == SmartMeterPinStrategy.IskraAsciiBlock;

            IDisposable? verificationSubscription = null;

            if (options.Verify)
            {
                verificationSubscription = _serialPort.Subscribe(new VerificationObserver(
                    options.ExpectedObisCodes,
                    expectAck,
                    (code, isAck) =>
                    {
                        if (isAck)
                        {
                            _logger.LogDebug("ACK byte (0x06) received from smart meter");
                        }
                        else if (code is not null && detected.Add(code))
                        {
                            _logger.LogDebug("Detected extended OBIS code {ObisCode}", code);
                        }

                        verificationTcs.TrySetResult(true);
                    }));
            }

            try
            {
                await SendPinAsync(pin, options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                verificationSubscription?.Dispose();

                _logger.LogWarning(ex, "Unlock cancelled while sending PIN");

                return new SmartMeterUnlockResult(
                    false, SmartMeterUnlockOutcome.Cancelled, [], stopwatch.Elapsed, "Cancelled");
            }
            catch (Exception ex)
            {
                verificationSubscription?.Dispose();

                _logger.LogError(ex, "Failed to send PIN to smart meter");

                return new SmartMeterUnlockResult(
                    false, SmartMeterUnlockOutcome.WriteFailed, [], stopwatch.Elapsed, ex.Message);
            }

            if (!options.Verify)
            {
                _logger.LogInformation(
                    "PIN sent, verification skipped by options. Elapsed={Elapsed}", stopwatch.Elapsed);

                return new SmartMeterUnlockResult(
                    true, SmartMeterUnlockOutcome.VerificationSkipped, [], stopwatch.Elapsed,
                    "Verification skipped");
            }

            _logger.LogInformation(
                "PIN sent, awaiting verification evidence (timeout={Timeout})", options.VerificationTimeout);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(options.VerificationTimeout);

            await using var _ = timeoutCts.Token.Register(() => verificationTcs.TrySetResult(false));

            var verified = await verificationTcs.Task.ConfigureAwait(false);

            verificationSubscription?.Dispose();

            stopwatch.Stop();

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Unlock cancelled while waiting for verification");

                return new SmartMeterUnlockResult(
                    false, SmartMeterUnlockOutcome.Cancelled, detected.ToArray(), stopwatch.Elapsed,
                    "Cancelled");
            }

            if (verified)
            {
                _logger.LogInformation(
                    "Smart meter unlocked. Detected codes: [{Codes}], elapsed={Elapsed}",
                    string.Join(", ", detected), stopwatch.Elapsed);

                return new SmartMeterUnlockResult(
                    true, SmartMeterUnlockOutcome.PinAccepted, detected.ToArray(), stopwatch.Elapsed);
            }

            _logger.LogWarning(
                "Unlock verification timed out after {Timeout}. No extended OBIS codes observed. " +
                "Possible causes: incorrect PIN, wrong strategy ({Strategy}) for this meter, " +
                "optical coupler not aligned, or serial port misconfigured.",
                options.VerificationTimeout, options.Strategy);

            return new SmartMeterUnlockResult(
                false, SmartMeterUnlockOutcome.VerificationTimeout, detected.ToArray(), stopwatch.Elapsed,
                "Verification timeout");
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Unlock cancelled");

            return new SmartMeterUnlockResult(
                false, SmartMeterUnlockOutcome.Cancelled, [], stopwatch.Elapsed, "Cancelled");
        }
    }

    private async Task SendPinAsync(string pin, SmartMeterUnlockOptions options, CancellationToken cancellationToken)
    {
        switch (options.Strategy)
        {
            case SmartMeterPinStrategy.EmhAsciiBlock:
            case SmartMeterPinStrategy.IskraAsciiBlock:
            {
                var payload = Encoding.ASCII.GetBytes(pin + options.LineEnding);

                _logger.LogDebug(
                    "Writing PIN as ASCII block ({Bytes} bytes, lineEnding={LineEndingLength}b)",
                    payload.Length, options.LineEnding.Length);

                _serialPort.Write(payload);

                break;
            }

            case SmartMeterPinStrategy.EasymeterDigitByDigit:
            {
                _logger.LogDebug(
                    "Writing PIN digit-by-digit ({Digits} digits, delay={Delay})",
                    pin.Length, options.DigitDelay);

                for (var i = 0; i < pin.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var digit = Encoding.ASCII.GetBytes(pin.AsSpan(i, 1).ToArray());

                    _serialPort.Write(digit);

                    if (i < pin.Length - 1 && options.DigitDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(options.DigitDelay, cancellationToken).ConfigureAwait(false);
                    }
                }

                break;
            }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(options), options.Strategy, "Unsupported PIN strategy");
        }
    }

    private void CloseSerialPort()
    {
        _logger.LogInformation("Closing serial port...");
        _serialPort.Close();
        _logger.LogInformation("Serial port closed");
    }

    private void DisposingSubscription()
    {
        if (_subscription == null)
        {
            return;
        }

        _logger.LogInformation("Disposing data producer subscription...");

        _subscription.Dispose();

        _logger.LogInformation("Subscription data producer disposed");

        _subscription = null;
    }

    /// <summary>
    /// Observes raw serial data and reports verification events
    /// (extended OBIS code detected or ACK byte received).
    /// </summary>
    private sealed class VerificationObserver : IObserver<byte[]>
    {
        private readonly IReadOnlyList<string> _expectedObisCodes;
        private readonly bool _expectAck;
        private readonly Action<string?, bool> _onHit;

        public VerificationObserver(
            IReadOnlyList<string> expectedObisCodes, bool expectAck, Action<string?, bool> onHit)
        {
            _expectedObisCodes = expectedObisCodes;
            _expectAck = expectAck;
            _onHit = onHit;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(byte[] value)
        {
            if (_expectAck && Array.IndexOf(value, (byte)0x06) >= 0)
            {
                _onHit(null, true);
            }

            foreach (var code in ObisCodeScanner.FindMatches(value, _expectedObisCodes))
            {
                _onHit(code, false);
            }
        }
    }

    public void Dispose()
    {
        _serialPort.Dispose();

        if (_subscription == null)
        {
            return;
        }

        _logger.LogDebug("Disposing subscription...");
        _subscription.Dispose();
        _logger.LogDebug("Subscription disposed");

        _subscription = null;
    }
}

using System.Reactive.Linq;
using CreativeCoders.Core;
using CreativeCoders.Core.Weak;
using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Parsing;
using CreativeCoders.SmartMeter.DataProcessing;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMeter.Server.Core;

public class SmartMeterReactiveDataPipeline : ISmartMeterReactiveDataPipeline
{
    private readonly ISmlParser _smlParser;
    private readonly ISmlMessageDetector _smlMessageDetector;
    private readonly ILogger<SmartMeterReactiveDataPipeline> _logger;

    public SmartMeterReactiveDataPipeline(ISmlParser smlParser, ISmlMessageDetector smlMessageDetector,
        ILogger<SmartMeterReactiveDataPipeline> logger)
    {
        _smlParser = Ensure.NotNull(smlParser);
        _smlMessageDetector = Ensure.NotNull(smlMessageDetector);
        _logger = Ensure.NotNull(logger);

        _smlMessageDetector.Messages.Subscribe(message =>
        {
            _logger.LogInformation("SML message detected. Length: {Length}", message.PayloadBytes.Length);
        });
    }

    public void OnCompleted()
    {
        //throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        //throw new NotImplementedException();
    }

    public void OnNext(byte[] value)
    {
        _logger.LogInformation("Received data. Length: {Length}", value.Length);

        _smlMessageDetector.Append(value);
    }

    public IDisposable Subscribe(IObserver<SmartMeterValue> observer)
    {
        _smlMessageDetector.Messages.Select(message => _smlParser.Parse(message.PayloadBytes)).Subscribe(smlMessage =>
        {
            _logger.LogInformation("Parsed SML message. Values count: {Count}", smlMessage.Values.Count);

            foreach (var value in smlMessage.Values)
            {
                _logger.LogInformation("Publishing value. Obis: {Obis}, Value: {Value}", value.ObisCode, value.Value);
            }
        });

        return new NullDisposable();
    }
}

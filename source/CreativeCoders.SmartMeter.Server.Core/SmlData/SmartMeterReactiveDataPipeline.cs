using System.Reactive.Linq;
using System.Reactive.Subjects;
using CreativeCoders.Core;
using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Parsing;
using CreativeCoders.SmartMeter.DataProcessing;
using CreativeCoders.SmartMeter.Sml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreativeCoders.SmartMeter.Server.Core.SmlData;

public class SmartMeterReactiveDataPipeline : ISmartMeterReactiveDataPipeline
{
    private const string ObisCodeEnergyActiveImport = "1-0:1.8.0";
    private const string ObisCodeEnergyActiveExport = "1-0:2.8.0";

    private readonly ISmlParser _smlParser;
    private readonly ISmlMessageDetector _smlMessageDetector;
    private readonly ILogger<SmartMeterReactiveDataPipeline> _logger;
    private readonly Subject<SmlValue> _valueSubject = new Subject<SmlValue>();

    private readonly SmartMeterOptions _smartMeterOptions;

    public SmartMeterReactiveDataPipeline(ISmlParser smlParser, ISmlMessageDetector smlMessageDetector,
        IOptions<SmartMeterOptions> smartMeterOptions, ILogger<SmartMeterReactiveDataPipeline> logger)
    {
        _smlParser = Ensure.NotNull(smlParser);
        _smlMessageDetector = Ensure.NotNull(smlMessageDetector);
        _logger = Ensure.NotNull(logger);
        _smartMeterOptions = Ensure.NotNull(smartMeterOptions).Value;

        _smlMessageDetector.Messages.Subscribe(message =>
        {
            _logger.LogDebug("SML message detected. Length: {Length}", message.PayloadBytes.Length);
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
        _smlMessageDetector.Append(value);
    }

    public IDisposable Subscribe(IObserver<SmartMeterValue> observer)
    {
        _smlMessageDetector.Messages.Select(message => _smlParser.Parse(message.PayloadBytes)).Subscribe(smlMessage =>
        {
            _logger.LogDebug("Parsed SML message. Values count: {Count}", smlMessage.Values.Count);

            foreach (var value in smlMessage.Values.Where(v => v.Value.HasValue))
            {
                if (value.ObisCode.StartsWith(ObisCodeEnergyActiveImport))
                {
                    _valueSubject.OnNext(new SmlValue(SmlValueType.PurchasedEnergy)
                        { Value = value.Value!.Value });
                }
                else if (value.ObisCode.StartsWith(ObisCodeEnergyActiveExport))
                {
                    _valueSubject.OnNext(new SmlValue(SmlValueType.SoldEnergy)
                        { Value = value.Value!.Value });
                }
            }
        });

        return _valueSubject.Select(value =>
            {
                if (value.ValueType == SmlValueType.PurchasedEnergy)
                {
                    return new SmlValue(SmlValueType.PurchasedEnergy)
                    {
                        Value = value.Value + _smartMeterOptions.PurchasedEnergyOffset
                    };
                }

                if (value.ValueType == SmlValueType.SoldEnergy)
                {
                    return new SmlValue(SmlValueType.SoldEnergy)
                    {
                        Value = value.Value + _smartMeterOptions.SoldEnergyOffset
                    };
                }

                return value;
            })
            .SelectSmartMeterValues()
            .Subscribe(observer);
    }
}

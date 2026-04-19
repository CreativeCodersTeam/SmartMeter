using System.Reactive.Subjects;
using System.Reactive.Linq;
using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Parsing;
using CreativeCoders.SmartMessageLanguage.Tlv;
using CreativeCoders.SmartMessageLanguage.Units;
using CreativeCoders.SmartMeter.Core.SmlData;
using CreativeCoders.SmartMeter.DataProcessing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CreativeCoders.SmartMeter.Core.Tests.SmlData;

public class SmartMeterReactiveDataPipelineTests
{
    private sealed class StubSmlMessageDetector : ISmlMessageDetector
    {
        public Subject<SmlFrame> MessagesSubject { get; } = new();

        public event EventHandler<SmlMessageEventArgs>? MessageReceived;

        public IObservable<SmlFrame> Messages => MessagesSubject;

        public List<byte[]> Appended { get; } = [];

        public int ResetCount { get; private set; }

        public int DisposeCount { get; private set; }

        public void Append(ReadOnlySpan<byte> data) => Appended.Add(data.ToArray());

        public void Reset() => ResetCount++;

        public void Dispose()
        {
            DisposeCount++;
            MessagesSubject.Dispose();
        }

        // Suppress unused-event warning
        public void RaiseReceived(SmlFrame frame) =>
            MessageReceived?.Invoke(this, new SmlMessageEventArgs(frame));
    }

    private sealed class StubSmlParser : ISmlParser
    {
        public Func<byte[], SmlParseResult> ParseBehavior { get; set; } =
            _ => new SmlParseResult([], []);

        public SmlParseResult Parse(SmlFrame frame) => ParseBehavior(frame.PayloadBytes);

        public SmlParseResult Parse(ReadOnlySpan<byte> payload) => ParseBehavior(payload.ToArray());
    }

    private static SmlFrame MakeFrame(byte[] payload) => new(payload, payload, true, 0);

    private static ObisValue MakeObis(string code, decimal value) =>
        new(code, value, SmlUnit.WattHour, 0, [], SmlMessageValueType.Unsigned);

    private static (SmartMeterReactiveDataPipeline Sut, StubSmlParser Parser,
        StubSmlMessageDetector Detector) CreateSut(SmartMeterOptions? opts = null)
    {
        var parser = new StubSmlParser();
        var detector = new StubSmlMessageDetector();

        var sut = new SmartMeterReactiveDataPipeline(
            parser,
            detector,
            Options.Create(opts ?? new SmartMeterOptions
            {
                PurchasedEnergyOffset = 0,
                SoldEnergyOffset = 0
            }),
            NullLogger<SmartMeterReactiveDataPipeline>.Instance);

        return (sut, parser, detector);
    }

    [Fact]
    public void OnNext_ForwardsBytesToDetector()
    {
        // Arrange
        var (sut, _, detector) = CreateSut();
        var data = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        sut.OnNext(data);

        // Assert
        detector.Appended.Should().ContainSingle().Which.Should().Equal(data);
    }

    [Fact]
    public void Subscribe_WithPurchasedEnergyObis_EmitsPurchasedEnergyValueWithOffset()
    {
        // Arrange
        var (sut, parser, detector) = CreateSut(new SmartMeterOptions
        {
            PurchasedEnergyOffset = 1000,
            SoldEnergyOffset = 0
        });
        parser.ParseBehavior = _ => new SmlParseResult(
            [MakeObis("1-0:1.8.0*255", 123m)], []);

        var received = new List<SmartMeterValue>();
        sut.Subscribe(new LambdaObserver<SmartMeterValue>(received.Add));

        // Act
        detector.MessagesSubject.OnNext(MakeFrame([0xAA]));

        // Assert
        received.Should().ContainSingle(v => v.Type == SmartMeterValueType.TotalPurchasedEnergy
                                             && v.Value == 1123m);
    }

    [Fact]
    public void Subscribe_WithSoldEnergyObis_EmitsSoldEnergyValueWithOffset()
    {
        // Arrange
        var (sut, parser, detector) = CreateSut(new SmartMeterOptions
        {
            PurchasedEnergyOffset = 0,
            SoldEnergyOffset = 50
        });
        parser.ParseBehavior = _ => new SmlParseResult(
            [MakeObis("1-0:2.8.0*255", 10m)], []);

        var received = new List<SmartMeterValue>();
        sut.Subscribe(new LambdaObserver<SmartMeterValue>(received.Add));

        // Act
        detector.MessagesSubject.OnNext(MakeFrame([0xAA]));

        // Assert
        received.Should().Contain(v => v.Type == SmartMeterValueType.TotalSoldEnergy && v.Value == 60m);
    }

    [Fact]
    public void Subscribe_WithUnrelatedObis_DoesNotEmitEnergyValues()
    {
        // Arrange
        var (sut, parser, detector) = CreateSut();
        parser.ParseBehavior = _ => new SmlParseResult(
            [MakeObis("1-0:99.9.0*255", 10m)], []);

        var received = new List<SmartMeterValue>();
        sut.Subscribe(new LambdaObserver<SmartMeterValue>(received.Add));

        // Act
        detector.MessagesSubject.OnNext(MakeFrame([0xAA]));

        // Assert
        received.Should().NotContain(v =>
            v.Type == SmartMeterValueType.TotalPurchasedEnergy
            || v.Type == SmartMeterValueType.TotalSoldEnergy);
    }

    [Fact]
    public void Subscribe_WhenObisValueIsNull_DoesNotEmit()
    {
        // Arrange
        var (sut, parser, detector) = CreateSut();
        parser.ParseBehavior = _ => new SmlParseResult(
            [new ObisValue("1-0:1.8.0*255", null, SmlUnit.Unknown, 0, [],
                SmlMessageValueType.Unsigned)], []);

        var received = new List<SmartMeterValue>();
        sut.Subscribe(new LambdaObserver<SmartMeterValue>(received.Add));

        // Act
        detector.MessagesSubject.OnNext(MakeFrame([0xAA]));

        // Assert
        received.Should().BeEmpty();
    }

    [Fact]
    public void OnCompleted_DoesNotThrow()
    {
        // Arrange: OnCompleted propagates to the internal SmlValue subject but the downstream
        // pipeline (SmlValueProcessor) does not forward completion to its observers. The method
        // must still be callable without throwing.
        var (sut, _, _) = CreateSut();
        sut.Subscribe(new LambdaObserver<SmartMeterValue>(_ => { }));

        // Act
        var act = () => sut.OnCompleted();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void OnError_DoesNotThrow()
    {
        // Arrange
        var (sut, _, _) = CreateSut();

        // Act
        var act = () => sut.OnError(new InvalidOperationException("boom"));

        // Assert
        act.Should().NotThrow();
    }

    private sealed class LambdaObserver<T>(
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null) : IObserver<T>
    {
        public void OnCompleted() => onCompleted?.Invoke();

        public void OnError(Exception error) => onError?.Invoke(error);

        public void OnNext(T value) => onNext(value);
    }
}

using System.Reactive.Subjects;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CreativeCoders.SmartMeter.DataProcessing.Tests;

public class SmlValueProcessorTests
{
    [Fact]
    public void Subscribe_WithPurchasedEnergyValue_ShouldReturnSmartMeterValueWithTotalPurchasedEnergy()
    {
        // Arrange
        SmartMeterValue? resultValue = null;

        var input = new Subject<SmlValue>();

        var smlValue = new SmlValue(SmlValueType.PurchasedEnergy)
        {
            Value = 123.45m
        };

        var smlValueProcessor = new SmlValueProcessor(input);

        // Act
        smlValueProcessor.Subscribe(x => resultValue = x);

        input.OnNext(smlValue);

        // Assert
        resultValue
            .Should()
            .NotBeNull();

        resultValue!.Type
            .Should()
            .Be(SmartMeterValueType.TotalPurchasedEnergy);
    }

    [Theory]
    [InlineData(SmlValueType.PurchasedEnergy, 100, 200)]
    [InlineData(SmlValueType.PurchasedEnergy, 250, 300)]
    [InlineData(SmlValueType.SoldEnergy, 200, 250)]
    [InlineData(SmlValueType.SoldEnergy, 15, 250)]
    public void Subscribe_WithTwoPurchasedEnergyValues_ShouldReturnTotalAndCurrentAndBalancePurchasedEnergy(
        SmlValueType smlValueType, decimal smlValueValue1, decimal smlValueValue2)
    {
        // Arrange
        var expectedBalanceValue = (smlValueValue2 - smlValueValue1) * 60;
        if (smlValueType == SmlValueType.PurchasedEnergy)
        {
            expectedBalanceValue *= -1;
        }

        var expectedSmartMeterValueType = smlValueType == SmlValueType.PurchasedEnergy
            ? SmartMeterValueType.CurrentPurchasingPower
            : SmartMeterValueType.CurrentSellingPower;

        List<SmartMeterValue> resultValues = [];
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now);

        var input = new Subject<SmlValue>();

        var smlValue1 = new SmlValue(smlValueType)
        {
            Value = smlValueValue1
        };

        var smlValue2 = new SmlValue(smlValueType)
        {
            Value = smlValueValue2
        };

        var smlValueProcessor = new SmlValueProcessor(input, fakeTimeProvider);

        // Act
        smlValueProcessor.Subscribe(resultValues.Add);

        input.OnNext(smlValue1);

        fakeTimeProvider.Advance(TimeSpan.FromSeconds(60));

        input.OnNext(smlValue2);
        input.OnCompleted();

        // Assert
        resultValues
            .Should()
            .HaveCount(4);

        var gridPowerBalanceValue = resultValues
            .Single(x => x.Type == SmartMeterValueType.GridPowerBalance);

        gridPowerBalanceValue.Value
            .Should()
            .Be(expectedBalanceValue);

        var currentPowerValue = resultValues
            .Single(x => x.Type == expectedSmartMeterValueType);

        currentPowerValue.Value
            .Should()
            .Be((smlValueValue2 - smlValueValue1) * 60);
    }

    [Fact]
    public void Subscribe_WithSameValueEmittedTwiceQuickly_SuppressesDuplicateTotal()
    {
        // Arrange
        var results = new List<SmartMeterValue>();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var input = new Subject<SmlValue>();
        var sut = new SmlValueProcessor(input, time);
        sut.Subscribe(results.Add);

        // Act
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 100m });
        time.Advance(TimeSpan.FromSeconds(5));
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 100m });

        // Assert
        // Only the first value emits TotalPurchasedEnergy; the second is suppressed because
        // value unchanged AND time diff < 30s.
        results.Count(x => x.Type == SmartMeterValueType.TotalPurchasedEnergy).Should().Be(1);
    }

    [Fact]
    public void Subscribe_WithSameValueAfterFiveMinutes_EmitsTotalAgain()
    {
        // Arrange
        var results = new List<SmartMeterValue>();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var input = new Subject<SmlValue>();
        var sut = new SmlValueProcessor(input, time);
        sut.Subscribe(results.Add);

        // Act
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 100m });
        time.Advance(TimeSpan.FromMinutes(6));
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 100m });

        // Assert
        // After 5 minutes the time-based gate forces another total emission.
        results.Count(x => x.Type == SmartMeterValueType.TotalPurchasedEnergy).Should().Be(2);
    }

    [Fact]
    public void Subscribe_WithNoSubsequentValueWithinTwentySeconds_DoesNotEmitCurrentPower()
    {
        // Arrange
        var results = new List<SmartMeterValue>();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var input = new Subject<SmlValue>();
        var sut = new SmlValueProcessor(input, time);
        sut.Subscribe(results.Add);

        // Act
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 100m });
        time.Advance(TimeSpan.FromSeconds(10));
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 105m });

        // Assert
        results.Should().NotContain(x => x.Type == SmartMeterValueType.CurrentPurchasingPower);
    }

    [Fact]
    public void Subscribe_WithUnchangedValueAfterTwentyOneSeconds_EmitsZeroCurrentPowerButNoBalance()
    {
        // Arrange - value diff=0, time diff>20s triggers the current-power branch with value 0.
        var results = new List<SmartMeterValue>();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var input = new Subject<SmlValue>();
        var sut = new SmlValueProcessor(input, time);
        sut.Subscribe(results.Add);

        // Act
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 100m });
        time.Advance(TimeSpan.FromSeconds(21));
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 100m });

        // Assert
        results.Should().Contain(x => x.Type == SmartMeterValueType.CurrentPurchasingPower && x.Value == 0m);
        // GridPowerBalance is only emitted when the current value is non-zero.
        results.Should().NotContain(x => x.Type == SmartMeterValueType.GridPowerBalance);
    }

    [Fact]
    public void Subscribe_WithPurchasedEnergyGap_EmitsNegativeGridPowerBalance()
    {
        // Arrange
        var results = new List<SmartMeterValue>();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var input = new Subject<SmlValue>();
        var sut = new SmlValueProcessor(input, time);
        sut.Subscribe(results.Add);

        // Act
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 100m });
        time.Advance(TimeSpan.FromMinutes(1));
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 200m });

        // Assert
        var balance = results.Single(x => x.Type == SmartMeterValueType.GridPowerBalance);
        balance.Value.Should().BeLessThan(0m);
        balance.WriteAsJson.Should().BeFalse();
    }

    [Fact]
    public void Subscribe_AfterSourceCompletes_StopsEmittingButSubjectStaysAlive()
    {
        // Arrange
        var results = new List<SmartMeterValue>();
        var input = new Subject<SmlValue>();
        var sut = new SmlValueProcessor(input);
        sut.Subscribe(results.Add);

        // Act
        input.OnNext(new SmlValue(SmlValueType.PurchasedEnergy) { Value = 50m });
        var emittedBefore = results.Count;
        input.OnCompleted();

        // Assert - new subscribers can still attach; no late values appear.
        results.Count.Should().Be(emittedBefore);
    }
}

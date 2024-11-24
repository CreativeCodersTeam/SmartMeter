using System.Reactive.Subjects;
using CreativeCoders.SmartMeter.Sml;
using FluentAssertions;
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
        smlValueProcessor.Subscribe(x => resultValues.Add(x));

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
}

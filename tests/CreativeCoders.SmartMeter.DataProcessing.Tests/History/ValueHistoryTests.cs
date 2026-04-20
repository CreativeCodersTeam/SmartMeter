using AwesomeAssertions;
using CreativeCoders.SmartMeter.DataProcessing.History;
using Xunit;

namespace CreativeCoders.SmartMeter.DataProcessing.Tests.History;

public class ValueHistoryTests
{
    [Fact]
    public void GetHistoryData_FirstCallForType_ReturnsEmptyInstance()
    {
        var sut = new ValueHistory();

        var data = sut.GetHistoryData(SmlValueType.PurchasedEnergy);

        data.Should().NotBeNull();
        data.DataSets.Should().BeEmpty();
        data.LastValue.Should().BeNull();
        data.LastValueTimeStamp.Should().BeNull();
    }

    [Fact]
    public void GetHistoryData_CalledTwiceForSameType_ReturnsSameInstance()
    {
        var sut = new ValueHistory();

        var first = sut.GetHistoryData(SmlValueType.PurchasedEnergy);
        var second = sut.GetHistoryData(SmlValueType.PurchasedEnergy);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetHistoryData_DifferentTypes_ReturnsIndependentInstances()
    {
        var sut = new ValueHistory();

        var purchased = sut.GetHistoryData(SmlValueType.PurchasedEnergy);
        var sold = sut.GetHistoryData(SmlValueType.SoldEnergy);

        sold.Should().NotBeSameAs(purchased);
    }

    [Fact]
    public void GetHistoryData_PreservesMutationsAcrossCalls()
    {
        var sut = new ValueHistory();
        var first = sut.GetHistoryData(SmlValueType.SoldEnergy);
        first.LastValue = new SmlValue(SmlValueType.SoldEnergy) { Value = 42m };

        var second = sut.GetHistoryData(SmlValueType.SoldEnergy);

        second.LastValue.Should().NotBeNull();
        second.LastValue!.Value.Should().Be(42m);
    }
}

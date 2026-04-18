using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Parsing;
using CreativeCoders.SmartMessageLanguage.Tests.Fixtures;
using CreativeCoders.SmartMessageLanguage.Units;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests.Parsing;

public class SmlParserTests
{
    [Fact]
    public void Parse_GetListResponsePayload_ExtractsAllObisValues()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();

        var parser = new SmlParser(NullLogger<SmlParser>.Instance);
        var result = parser.Parse(payload);

        result.Warnings.Should().BeEmpty();
        result.Values.Should().HaveCount(2);

        var energy = result.Values.Single(v => v.ObisCode == "1-0:1.8.0*255");
        energy.Unit.Should().Be(SmlUnit.WattHour);
        energy.Scaler.Should().Be((sbyte)-1);
        energy.Value.Should().Be(12345.6m);

        var power = result.Values.Single(v => v.ObisCode == "1-0:16.7.0*255");
        power.Unit.Should().Be(SmlUnit.Watt);
        power.Scaler.Should().Be((sbyte)0);
        power.Value.Should().Be(567m);
    }

    [Fact]
    public void Parse_EmptyPayload_ReturnsEmptyResult()
    {
        var parser = new SmlParser(NullLogger<SmlParser>.Instance);

        var result = parser.Parse([]);

        result.Values.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }
}

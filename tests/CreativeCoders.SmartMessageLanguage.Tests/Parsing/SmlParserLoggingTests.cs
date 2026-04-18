using AwesomeAssertions;
using CreativeCoders.SmartMessageLanguage.Parsing;
using CreativeCoders.SmartMessageLanguage.Tests.Fixtures;
using CreativeCoders.SmartMessageLanguage.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CreativeCoders.SmartMessageLanguage.Tests.Parsing;

public class SmlParserLoggingTests
{
    [Fact]
    public void Parse_ValidGetListResponse_LogsValuesAndCompletion()
    {
        var payload = SampleSmlFile.BuildGetListResponsePayload();
        var logger = LoggerCallAssertions.CreateEnabledLogger<SmlParser>();
        var parser = new SmlParser(logger);

        var result = parser.Parse(payload);

        result.Values.Should().HaveCount(2);
        // ParseStarted + GetListResponseFound + 2x ObisValueParsed = 4 Debug entries minimum.
        LoggerCallAssertions.CountCalls(logger, LogLevel.Debug, 2001).Should().Be(1);
        LoggerCallAssertions.CountCalls(logger, LogLevel.Debug, 2002).Should().Be(1);
        LoggerCallAssertions.CountCalls(logger, LogLevel.Debug, 2003).Should().Be(2);
        LoggerCallAssertions.CountCalls(logger, LogLevel.Debug, 2004).Should().Be(1);
        LoggerCallAssertions.CountCalls(logger, LogLevel.Warning).Should().Be(0);
        LoggerCallAssertions.CountCalls(logger, LogLevel.Error).Should().Be(0);
    }
}

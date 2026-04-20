using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace CreativeCoders.SmartMessageLanguage.Tests.TestSupport;

// Helpers to inspect calls made by source-generated [LoggerMessage] partials.
// These funnel through ILogger.Log<TState>(level, eventId, state, exception, formatter).
internal static class LoggerCallAssertions
{
    public static int CountCalls<T>(ILogger<T> logger, LogLevel level)
    {
        return Fake.GetCalls(logger)
            .Where(call => call.Method.Name == nameof(ILogger.Log))
            .Count(call => Equals(call.Arguments[0], level));
    }

    public static int CountCalls<T>(ILogger<T> logger, LogLevel level, int eventId)
    {
        return Fake.GetCalls(logger)
            .Where(call => call.Method.Name == nameof(ILogger.Log))
            .Where(call => Equals(call.Arguments[0], level))
            .Count(call => call.Arguments[1] is EventId id && id.Id == eventId);
    }

    public static ILogger<T> CreateEnabledLogger<T>()
    {
        var logger = A.Fake<ILogger<T>>();
        A.CallTo(() => logger.IsEnabled(A<LogLevel>._)).Returns(true);

        return logger;
    }
}

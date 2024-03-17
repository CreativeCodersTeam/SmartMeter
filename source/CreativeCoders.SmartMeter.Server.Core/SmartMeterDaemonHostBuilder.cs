using CreativeCoders.Daemon;
using CreativeCoders.SmartMeter.DataProcessing;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace CreativeCoders.SmartMeter.Server.Core;

public static class SmartMeterDaemonHostBuilder
{
    public static IDaemonHostBuilder CreateSmartMeterDaemonHostBuilder(string[] args)
    {
        return DaemonHostBuilder
            .CreateBuilder<SmartMeterServer>()
            .WithArgs(args)
            .ConfigureServices(ConfigureServices)
            .ConfigureHostBuilder(x =>
                x.UseSerilog((_, conf) => conf.WriteTo.Console(LogEventLevel.Information)));
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();
    }
}
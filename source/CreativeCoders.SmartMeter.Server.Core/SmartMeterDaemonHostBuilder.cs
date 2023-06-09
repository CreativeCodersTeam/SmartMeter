﻿using CreativeCoders.Daemon;
using CreativeCoders.SmartMeter.DataProcessing;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
                x.UseSerilog((context, conf) => conf.WriteTo.Console()));
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSmartMeter();
    }
}
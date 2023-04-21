using CreativeCoders.Daemon.Linux;
using CreativeCoders.SmartMeter.DataProcessing;
using CreativeCoders.SmartMeter.Server.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

await SmartMeterDaemonHostBuilder.CreateSmartMeterDaemonHostBuilder(args)
    .ConfigureHostBuilder(x =>
        x.ConfigureAppConfiguration((_, configBuilder) =>
            configBuilder.AddJsonFile("/etc/smartmeter.conf")))
    .ConfigureServices(services =>
    {
        var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services.Configure<MqttPublisherOptions>(config.GetSection("Mqtt"));
    })
    .WithDefinitionFile("daemon.json")
    .UseSystemd()
    .Build()
    .RunAsync()
    .ConfigureAwait(false);

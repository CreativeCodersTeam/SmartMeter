using CreativeCoders.Daemon.Linux;
using CreativeCoders.SmartMeter.Server.Core;

await SmartMeterDaemonHostBuilder.CreateSmartMeterDaemonHostBuilder(args)
    .WithDefinitionFile("daemon.json")
    .UseSystemd()
    .Build()
    .RunAsync()
    .ConfigureAwait(false);

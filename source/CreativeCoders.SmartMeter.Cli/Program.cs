using CreativeCoders.SmartMeter.DataProcessing;
using CreativeCoders.SmartMeter.Server.Core;
using CreativeCoders.SmartMeter.Server.Core.SmlData;
using CreativeCoders.SmartMeter.Server.Core.Unlock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace CreativeCoders.SmartMeter.Cli;

class Program
{
    static async Task Main(string[] args)
    {
        AnsiConsole.WriteLine("Starting Smart Meter CLI...");

        var sp = new ServiceCollection()
            .AddLogging(configure => configure.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            }))
            .AddSingleton<ISmartMeterDataProducer, SmartMeterDataProducer>()
            .AddSmartMeterServer()
            .BuildServiceProvider();

        var dataProducer = sp.GetRequiredService<ISmartMeterDataProducer>();

        if (args.Length > 1 && args[0].Equals("unlock", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.WriteLine("Unlocking Smart Meter with provided PIN...");

            var pin = args[1];
            var unlocker = sp.GetRequiredService<ISmartMeterUnlocker>();

            await SendPinAsync(unlocker, pin);

            return;
        }

        await dataProducer.StartAsync(new SmartMeterConsoleOutput());

        AnsiConsole.WriteLine("Press any key to stop...");
        await AnsiConsole.Console.Input.ReadKeyAsync(false, CancellationToken.None);

        await dataProducer.StopAsync();

        AnsiConsole.WriteLine("Smart Meter CLI stopped");
    }

    private static async Task SendPinAsync(ISmartMeterUnlocker unlocker, string pin)
    {
        AnsiConsole.WriteLine($"Sending PIN: {pin}");
        await unlocker.UnlockAsync(pin, new SmartMeterUnlockOptions
        {
            Strategy = SmartMeterPinStrategy.EmhAsciiBlock
        });
    }
}

internal class SmartMeterConsoleOutput : IObserver<SmartMeterValue>
{
    public void OnCompleted()
    {
        AnsiConsole.WriteLine("Data stream completed");
    }

    public void OnError(Exception error)
    {
        AnsiConsole.WriteLine($"Error: {error.Message}");
    }

    public void OnNext(SmartMeterValue value)
    {
        AnsiConsole.WriteLine($"Received value: {value.Type} = {value.Value:N}");
    }
}

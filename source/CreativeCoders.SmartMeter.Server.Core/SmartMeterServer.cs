using CreativeCoders.Core;
using CreativeCoders.Daemon;
using CreativeCoders.SmartMeter.DataProcessing;
using JetBrains.Annotations;

namespace CreativeCoders.SmartMeter.Server.Core;

[UsedImplicitly]
public class SmartMeterServer : IDaemonService
{
    private readonly ISmartMeterDataProcessor _smartMeterDataProcessor;
    
    private readonly CancellationTokenSource _stoppingTokenSource;
    
    private Task? _runningTask;

    public SmartMeterServer(ISmartMeterDataProcessor smartMeterDataProcessor)
    {
        _smartMeterDataProcessor = Ensure.NotNull(smartMeterDataProcessor, nameof(smartMeterDataProcessor));

        _stoppingTokenSource = new CancellationTokenSource();
    }
    
    public Task StartAsync()
    {
        if (_runningTask != null)
        {
            return Task.CompletedTask;
        }
        
        _runningTask = _smartMeterDataProcessor.RunAsync(_stoppingTokenSource.Token);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_runningTask == null)
        {
            return;
        }
        
        _runningTask.RunSynchronously();
        _stoppingTokenSource.Cancel();

        await _runningTask;

        _runningTask = null;
    }
}

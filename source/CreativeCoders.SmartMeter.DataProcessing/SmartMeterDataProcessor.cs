using CreativeCoders.Core;
using CreativeCoders.SmartMeter.Sml;

namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmartMeterDataProcessor : ISmartMeterDataProcessor
{
    private readonly ISmlSerialPortTransport _serialPortTransport;
    
    private readonly ISmlInputStream _inputStream;
    
    private Func<SmartMeterValue,Task> _handleValueAsync = _ => Task.CompletedTask;

    public SmartMeterDataProcessor(ISmlSerialPortTransport serialPortTransport, ISmlInputStream inputStream)
    {
        _serialPortTransport = Ensure.NotNull(serialPortTransport, nameof(serialPortTransport));
        _inputStream = Ensure.NotNull(inputStream, nameof(inputStream));
    }
    
    public ISmartMeterDataProcessor HandleValue(Func<SmartMeterValue, Task> handleValueAsync)
    {
        Ensure.NotNull(handleValueAsync, nameof(handleValueAsync));
        
        _handleValueAsync = handleValueAsync;

        return this;
    }

    private void HandleMessageAsync(SmlMessage message)
    {
        
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
#pragma warning disable CS4014
        Task.Run(() => _inputStream.HandleSmlMessages(HandleMessageAsync), cancellationToken);
#pragma warning restore CS4014
        
        _serialPortTransport.StartProcessing();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
        
        _serialPortTransport.StopProcessing();
    }
}

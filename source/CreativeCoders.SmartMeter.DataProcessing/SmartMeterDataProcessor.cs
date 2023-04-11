namespace CreativeCoders.SmartMeter.DataProcessing;

public class SmartMeterDataProcessor : ISmartMeterDataProcessor
{
    public ISmartMeterDataProcessor HandleValue(Func<SmartMeterValue, Task> handleValueAsync)
    {
        throw new NotImplementedException();
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

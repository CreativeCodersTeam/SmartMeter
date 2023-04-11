namespace CreativeCoders.SmartMeter.DataProcessing;

public interface ISmartMeterDataProcessor
{
    ISmartMeterDataProcessor HandleValue(Func<SmartMeterValue, Task> handleValueAsync);

    Task RunAsync(CancellationToken cancellationToken);
}
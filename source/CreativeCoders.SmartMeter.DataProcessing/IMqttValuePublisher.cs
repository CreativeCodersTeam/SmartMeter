namespace CreativeCoders.SmartMeter.DataProcessing;

/// <summary>
/// Observer that publishes <see cref="SmartMeterValue"/> instances to an MQTT broker.
/// </summary>
public interface IMqttValuePublisher : IObserver<SmartMeterValue>
{
    /// <summary>Connects to the broker and starts the background publishing loop.</summary>
    Task InitAsync();
}

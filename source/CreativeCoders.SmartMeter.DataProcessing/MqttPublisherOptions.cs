namespace CreativeCoders.SmartMeter.DataProcessing;

public class MqttPublisherOptions
{
    public Uri? Server { get; set; }

    public string ClientName { get; set; } = "SmartMeterClient";

    public string TopicTemplate { get; set; } = "smartmeter/values/{0}";
}

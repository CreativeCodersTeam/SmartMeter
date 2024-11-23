using JetBrains.Annotations;

namespace CreativeCoders.SmartMeter.DataProcessing;

[PublicAPI]
public class MqttPublisherOptions
{
    public Uri? Server { get; set; }

    public string ClientName { get; set; } = "SmartMeterClient";

    public string TopicTemplate { get; set; } = "smartmeter/values/{0}";
}

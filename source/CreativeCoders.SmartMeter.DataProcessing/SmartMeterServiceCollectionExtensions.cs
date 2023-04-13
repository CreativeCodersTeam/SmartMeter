using CreativeCoders.SmartMeter.Sml;
using Microsoft.Extensions.DependencyInjection;

namespace CreativeCoders.SmartMeter.DataProcessing;

public static class SmartMeterServiceCollectionExtensions
{
    public static void AddSmartMeter(this IServiceCollection services)
    {
        services.AddSml();

        services.AddSingleton<ISmartMeterDataProcessor, SmartMeterDataProcessor>();
    }
}

using CreativeCoders.SmartMessageLanguage;
using CreativeCoders.SmartMeter.Server.Core.SmlData;
using CreativeCoders.SmartMeter.Server.Core.Unlock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CreativeCoders.SmartMeter.Server.Core;

public static class SmartMeterServerServiceCollectionExtensions
{
    public static IServiceCollection AddSmartMeterServer(this IServiceCollection services)
    {
        services.AddSml();
        services.TryAddSingleton<ISmartMeterReactiveDataPipeline, SmartMeterReactiveDataPipeline>();
        services.TryAddSingleton<ISmartMeterUnlocker, SmartMeterUnlocker>();

        return services;
    }
}

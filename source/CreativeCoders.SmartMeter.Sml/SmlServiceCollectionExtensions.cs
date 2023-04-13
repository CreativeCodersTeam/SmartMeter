using System.Runtime.CompilerServices;
using CreativeCoders.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CreativeCoders.SmartMeter.Sml;

public static class SmlServiceCollectionExtensions
{
    public static void AddSml(this IServiceCollection services)
    {
        Ensure.NotNull(services, nameof(services));
        
        services.AddSingleton<ISmlValueReader, SmlValueReader>();
        services.AddSingleton<ISmlInputStream, SmlInputStream>();
        services.AddSingleton<ISmlSerialPortTransport, SmlSerialPortTransport>();
    }
}

using CreativeCoders.SmartMessageLanguage.Framing;
using CreativeCoders.SmartMessageLanguage.Parsing;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CreativeCoders.SmartMessageLanguage;

[PublicAPI]
public static class SmlServiceCollectionExtensions
{
    public static IServiceCollection AddSml(this IServiceCollection services)
    {
        services.TryAddSingleton<ISmlParser, SmlParser>();
        services.TryAddSingleton<ISmlMessageDetector, SmlMessageDetector>();

        return services;
    }
}

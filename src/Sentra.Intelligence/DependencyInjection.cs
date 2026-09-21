using Microsoft.Extensions.DependencyInjection;
using Sentra.Application.Intelligence;

namespace Sentra.Intelligence;

public static class DependencyInjection
{
    public static IServiceCollection AddSentraIntelligence(this IServiceCollection services)
    {
        services.AddSingleton<IIntelligenceEngine, SentraIntelligenceEngine>();
        return services;
    }
}

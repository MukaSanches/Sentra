using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSentraInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<SentraDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}

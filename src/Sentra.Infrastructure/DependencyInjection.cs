using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSentraInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgres = configuration.GetConnectionString("Postgres")
            ?? configuration["DATABASE_CONNECTION_STRING"];

        if (!string.IsNullOrWhiteSpace(postgres))
        {
            services.AddDbContext<SentraDbContext>(options =>
                options.UseNpgsql(
                    postgres,
                    npgsql => npgsql.MigrationsAssembly(typeof(SentraDbContext).Assembly.FullName)));
        }

        return services;
    }
}

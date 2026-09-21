using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentra.Application.Security;
using Sentra.Infrastructure.Persistence;
using Sentra.Infrastructure.Operations;
using Sentra.Application.Operations;
using Sentra.Infrastructure.Security;

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
            services.AddHostedService<PermissionCatalogSynchronizer>();
        }

        services.AddScoped<IOperationalStore, OperationalStore>();
        services.AddSingleton<IPasswordHashService, AspNetPasswordHashService>();

        return services;
    }
}

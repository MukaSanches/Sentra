using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sentra.Application.Security;
using Sentra.Infrastructure.Persistence;
using Sentra.Infrastructure.Security;

namespace Sentra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSentraInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<SentraDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IPasswordHashService, Pbkdf2PasswordHashService>();

        return services;
    }
}

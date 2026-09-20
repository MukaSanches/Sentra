using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sentra.Api.Health;

public sealed class SetupReadinessHealthCheck(IConfiguration configuration)
    : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        var postgres = configuration.GetConnectionString("Postgres")
            ?? configuration["DATABASE_CONNECTION_STRING"];

        var authMode = configuration["Authentication:Mode"]
            ?? configuration["AUTH_MODE"];

        if (string.IsNullOrWhiteSpace(postgres))
        {
            missing.Add("DATABASE_CONNECTION_STRING");
        }

        if (string.Equals(authMode, "Local", StringComparison.OrdinalIgnoreCase))
        {
            Require(configuration, missing, "Authentication:Issuer", "AUTH_ISSUER");
            Require(configuration, missing, "Authentication:Audience", "AUTH_AUDIENCE");

            var signingKey = configuration["Authentication:SigningKey"]
                ?? configuration["AUTH_SIGNING_KEY"];

            if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
            {
                missing.Add("AUTH_SIGNING_KEY(min 32)");
            }
        }
        else if (string.Equals(authMode, "External", StringComparison.OrdinalIgnoreCase))
        {
            Require(configuration, missing, "Authentication:Authority", "AUTHORITY");
            Require(configuration, missing, "Authentication:Audience", "AUTH_AUDIENCE");
        }
        else
        {
            missing.Add("AUTH_MODE(Local|External)");
        }

        return Task.FromResult(
            missing.Count == 0
                ? HealthCheckResult.Healthy("Configuração base presente.")
                : HealthCheckResult.Degraded(
                    $"AGUARDANDO CONFIGURAÇÃO: {string.Join(", ", missing)}"));
    }

    private static void Require(
        IConfiguration configuration,
        ICollection<string> missing,
        string configurationKey,
        string environmentKey)
    {
        var value = configuration[configurationKey]
            ?? configuration[environmentKey];

        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add(environmentKey);
        }
    }
}

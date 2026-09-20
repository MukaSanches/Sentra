using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sentra.Api.Health;

public sealed class SetupReadinessHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        var postgres = configuration.GetConnectionString("Postgres")
            ?? configuration["DATABASE_CONNECTION_STRING"];
        var authority = configuration["Authentication:Authority"]
            ?? configuration["AUTHORITY"];
        var audience = configuration["Authentication:Audience"]
            ?? configuration["AUTH_AUDIENCE"];

        if (string.IsNullOrWhiteSpace(postgres))
        {
            missing.Add("DATABASE_CONNECTION_STRING");
        }

        if (string.IsNullOrWhiteSpace(authority))
        {
            missing.Add("AUTHORITY");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            missing.Add("AUTH_AUDIENCE");
        }

        return Task.FromResult(
            missing.Count == 0
                ? HealthCheckResult.Healthy("Foundation configuration is present.")
                : HealthCheckResult.Degraded($"AGUARDANDO CONFIGURAÇÃO: {string.Join(", ", missing)}"));
    }
}

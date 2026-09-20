using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sentra.Api.Health;

public sealed class SetupReadinessHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Postgres")))
        {
            missing.Add("ConnectionStrings:Postgres");
        }

        if (string.IsNullOrWhiteSpace(configuration["Authentication:Authority"]))
        {
            missing.Add("Authentication:Authority");
        }

        if (string.IsNullOrWhiteSpace(configuration["Authentication:Audience"]))
        {
            missing.Add("Authentication:Audience");
        }

        return Task.FromResult(
            missing.Count == 0
                ? HealthCheckResult.Healthy("Foundation configuration is present.")
                : HealthCheckResult.Degraded($"AGUARDANDO CONFIGURAÇÃO: {string.Join(", ", missing)}"));
    }
}

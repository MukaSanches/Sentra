using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sentra.Infrastructure.Persistence;

public sealed class SentraDbContextFactory : IDesignTimeDbContextFactory<SentraDbContext>
{
    public SentraDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DATABASE_CONNECTION_STRING deve ser configurada para executar o tooling de migrations.");
        }

        var options = new DbContextOptionsBuilder<SentraDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SentraDbContext(options);
    }
}

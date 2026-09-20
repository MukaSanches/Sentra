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
                "DATABASE_CONNECTION_STRING is required to run EF Core design-time operations.");
        }

        var options = new DbContextOptionsBuilder<SentraDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(SentraDbContext).Assembly.FullName))
            .Options;

        return new SentraDbContext(options);
    }
}

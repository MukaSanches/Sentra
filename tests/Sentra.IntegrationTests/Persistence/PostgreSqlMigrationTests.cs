using Microsoft.EntityFrameworkCore;
using Sentra.Domain.Properties;
using Sentra.Infrastructure.Persistence;

namespace Sentra.IntegrationTests.Persistence;

public sealed class PostgreSqlMigrationTests
{
    [Fact]
    public async Task Migrations_ApplyFromEmptyDatabase_AndEnforceCoreConstraints()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var options = new DbContextOptionsBuilder<SentraDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(SentraDbContext).Assembly.FullName))
            .Options;

        await using (var database = new SentraDbContext(options))
        {
            await database.Database.MigrateAsync(cancellationToken);

            var pending = await database.Database.GetPendingMigrationsAsync(cancellationToken);
            Assert.Empty(pending);

            Assert.Equal(7, await database.Permissions.CountAsync(cancellationToken));

            var condominium = new Condominium("Condomínio CI", "ci", DateTimeOffset.UtcNow);
            database.Condominiums.Add(condominium);
            await database.SaveChangesAsync(cancellationToken);

            database.Blocks.Add(
                new Block(
                    condominium.Id,
                    "Bloco A",
                    "ci",
                    "A",
                    DateTimeOffset.UtcNow));
            await database.SaveChangesAsync(cancellationToken);
        }

        await using (var duplicateContext = new SentraDbContext(options))
        {
            var condominiumId = await duplicateContext.Condominiums
                .Select(x => x.Id)
                .SingleAsync(cancellationToken);

            duplicateContext.Blocks.Add(
                new Block(
                    condominiumId,
                    "Bloco A",
                    "ci",
                    "A2",
                    DateTimeOffset.UtcNow));

            await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicateContext.SaveChangesAsync(cancellationToken));
        }
    }
}

using System.IO;
using System.Text.Json;
using Sentra.Contracts.Operations;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.Tests.Services;

public sealed class OfflineCacheServiceTests
{
    [Fact]
    public async Task Snapshot_RoundTrip_PreservesOperationalData()
    {
        var path = CreateTempPath();
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var service = new OfflineCacheService(path);
            var unitId = Guid.NewGuid();
            var snapshot = new OfflineSnapshotResponse(
                new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero),
                [
                    new OfflineResidentResponse(
                        Guid.NewGuid(),
                        "Maria Souza",
                        "+5511999999999",
                        unitId,
                        "Apto 101")
                ],
                [],
                [],
                []);

            await service.SaveSnapshotAsync(snapshot, cancellationToken);
            var loaded = await service.LoadSnapshotAsync(cancellationToken);

            Assert.NotNull(loaded);
            Assert.Single(loaded.Residents);
            Assert.Equal("Maria Souza", loaded.Residents[0].FullName);
            Assert.Equal(unitId, loaded.Residents[0].UnitId);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task Outbox_QueuesAndRemovesOnlyConfirmedItem()
    {
        var path = CreateTempPath();
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var service = new OfflineCacheService(path);

            await service.QueueAsync(
                "post",
                "/api/v1/occurrences",
                new { title = "Portão travado" },
                cancellationToken);

            var pending = await service.GetPendingAsync(cancellationToken);
            var item = Assert.Single(pending);

            Assert.Equal("POST", item.Method);
            Assert.Equal("api/v1/occurrences", item.Path);
            Assert.NotNull(item.JsonBody);
            using var document = JsonDocument.Parse(item.JsonBody);
            Assert.Equal(
                "Portão travado",
                document.RootElement.GetProperty("title").GetString());

            await service.MarkSyncedAsync(item.Id, cancellationToken);

            Assert.Empty(await service.GetPendingAsync(cancellationToken));
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string CreateTempPath()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sentra-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "offline.db");
    }

    private static void TryDelete(string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch
        {
        }
    }
}

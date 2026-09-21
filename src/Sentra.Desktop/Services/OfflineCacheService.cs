using System.Net.Http;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Sentra.Contracts.Operations;

namespace Sentra.Desktop.Services;

public sealed record OfflineOutboxItem(
    Guid Id,
    string Method,
    string Path,
    string? JsonBody,
    DateTimeOffset CreatedAt);

public interface IOfflineCacheService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(OfflineSnapshotResponse snapshot, CancellationToken cancellationToken = default);
    Task<OfflineSnapshotResponse?> LoadSnapshotAsync(CancellationToken cancellationToken = default);
    Task QueueAsync(string method, string path, object? body, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OfflineOutboxItem>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task MarkSyncedAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class OfflineCacheService : IOfflineCacheService
{
    private readonly string _connectionString;

    public OfflineCacheService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SENTRA",
            "offline.db"))
    {
    }

    public OfflineCacheService(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
CREATE TABLE IF NOT EXISTS cache_snapshot(
    cache_key TEXT PRIMARY KEY,
    json TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS outbox(
    id TEXT PRIMARY KEY,
    method TEXT NOT NULL,
    path TEXT NOT NULL,
    json_body TEXT NULL,
    created_at TEXT NOT NULL,
    status TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_outbox_status_created ON outbox(status, created_at);
""";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveSnapshotAsync(OfflineSnapshotResponse snapshot, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO cache_snapshot(cache_key, json, updated_at)
VALUES('latest', $json, $updated_at)
ON CONFLICT(cache_key) DO UPDATE SET json=excluded.json, updated_at=excluded.updated_at;
""";
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(snapshot));
        command.Parameters.AddWithValue("$updated_at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<OfflineSnapshotResponse?> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT json FROM cache_snapshot WHERE cache_key='latest';";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string json
            ? JsonSerializer.Deserialize<OfflineSnapshotResponse>(json)
            : null;
    }

    public async Task QueueAsync(string method, string path, object? body, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO outbox(id, method, path, json_body, created_at, status)
VALUES($id, $method, $path, $body, $created_at, 'Pending');
""";
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
        command.Parameters.AddWithValue("$method", method.ToUpperInvariant());
        command.Parameters.AddWithValue("$path", path.TrimStart('/'));
        command.Parameters.AddWithValue("$body", body is null ? DBNull.Value : JsonSerializer.Serialize(body));
        command.Parameters.AddWithValue("$created_at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfflineOutboxItem>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT id, method, path, json_body, created_at
FROM outbox
WHERE status='Pending'
ORDER BY created_at
LIMIT 100;
""";
        var items = new List<OfflineOutboxItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new OfflineOutboxItem(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                DateTimeOffset.Parse(reader.GetString(4))));
        }
        return items;
    }

    public async Task MarkSyncedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM outbox WHERE id=$id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class OfflineSyncService(
    IOfflineCacheService cache,
    ISentraApiClient api,
    SessionState session)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await cache.InitializeAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        do
        {
            if (session.IsAuthenticated)
            {
                await SynchronizeOnceAsync(stoppingToken);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SynchronizeOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await api.IsAliveAsync(cancellationToken))
            {
                return;
            }

            var snapshot = await api.GetOfflineSnapshotAsync(cancellationToken);
            await cache.SaveSnapshotAsync(snapshot, cancellationToken);

            var pending = await cache.GetPendingAsync(cancellationToken);
            foreach (var item in pending)
            {
                await api.SendOutboxAsync(item.Method, item.Path, item.JsonBody, cancellationToken);
                await cache.MarkSyncedAsync(item.Id, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or SentraApiException or OperationCanceledException)
        {
            // Mantém a fila local intacta para a próxima tentativa.
        }
    }
}

using Microsoft.AspNetCore.SignalR.Client;

namespace Sentra.Desktop.Services;

public sealed class RealtimeService(
    IDesktopSettingsService settingsService,
    SessionState sessionState) : IAsyncDisposable
{
    private HubConnection? _connection;

    public event EventHandler? ConversationChanged;

    public string State =>
        _connection?.State.ToString() ?? "Disconnected";

    public async Task ConnectAsync(
        CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(cancellationToken);

        var settings = await settingsService.LoadAsync(cancellationToken);
        var baseUri = settings.TryGetApiBaseUri()
            ?? throw new InvalidOperationException(
                "Configure a URL válida do servidor.");

        if (!sessionState.IsAuthenticated)
        {
            throw new InvalidOperationException(
                "Faça login antes de conectar o tempo real.");
        }

        var hubUri = new Uri(baseUri, "hubs/operations");

        _connection = new HubConnectionBuilder()
            .WithUrl(
                hubUri,
                options =>
                {
                    options.AccessTokenProvider =
                        () => Task.FromResult(sessionState.AccessToken);
                })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<object>(
            "ConversationChanged",
            _ => ConversationChanged?.Invoke(this, EventArgs.Empty));

        await _connection.StartAsync(cancellationToken);
    }

    public async Task DisconnectAsync(
        CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            return;
        }

        try
        {
            await _connection.StopAsync(cancellationToken);
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}

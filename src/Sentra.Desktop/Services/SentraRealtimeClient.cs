using Microsoft.AspNetCore.SignalR.Client;

namespace Sentra.Desktop.Services;

public sealed class SentraRealtimeClient(
    IDesktopSettingsService settingsService,
    IAccessTokenProvider accessTokenProvider) : ISentraRealtimeClient, IAsyncDisposable
{
    private HubConnection? _connection;
    private string? _connectionKey;

    public event EventHandler? WhatsAppChanged;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        if (!settings.HasServerConfiguration || settings.CondominiumId is null)
            return;

        var token = await accessTokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return;

        var key = $"{settings.ApiBaseUrl}|{settings.CondominiumId:D}";
        if (_connection is not null
            && _connectionKey == key
            && _connection.State == HubConnectionState.Connected)
        {
            return;
        }

        if (_connection is not null)
            await _connection.DisposeAsync();

        var hubUri = new Uri(
            new Uri(settings.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute),
            "hubs/operations");

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.AccessTokenProvider = async () =>
                    await accessTokenProvider.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<Guid, Guid, Guid>(
            "WhatsAppMessageReceived",
            (_, _, _) => WhatsAppChanged?.Invoke(this, EventArgs.Empty));

        _connection.On<Guid, Guid, Guid>(
            "WhatsAppMessageStatusChanged",
            (_, _, _) => WhatsAppChanged?.Invoke(this, EventArgs.Empty));

        await _connection.StartAsync(cancellationToken);
        await _connection.InvokeAsync(
            "JoinCondominium",
            settings.CondominiumId.Value,
            cancellationToken);

        _connectionKey = key;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}

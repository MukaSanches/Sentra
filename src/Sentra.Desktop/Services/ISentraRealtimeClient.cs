namespace Sentra.Desktop.Services;

public interface ISentraRealtimeClient
{
    event EventHandler? WhatsAppChanged;

    Task ConnectAsync(CancellationToken cancellationToken = default);
}

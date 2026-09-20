namespace Sentra.Application.Realtime;

public interface IRealtimeOperationsNotifier
{
    Task WhatsAppMessageReceivedAsync(
        Guid condominiumId,
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken);

    Task WhatsAppMessageStatusChangedAsync(
        Guid condominiumId,
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken);
}

using Microsoft.AspNetCore.SignalR;
using Sentra.Application.Realtime;

namespace Sentra.Api.Realtime;

public sealed class SignalROperationsNotifier(IHubContext<OperationsHub> hubContext)
    : IRealtimeOperationsNotifier
{
    public Task WhatsAppMessageReceivedAsync(
        Guid condominiumId,
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken)
        => hubContext.Clients.Group(OperationsHub.GroupName(condominiumId))
            .SendAsync(
                "WhatsAppMessageReceived",
                condominiumId,
                conversationId,
                messageId,
                cancellationToken);

    public Task WhatsAppMessageStatusChangedAsync(
        Guid condominiumId,
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken)
        => hubContext.Clients.Group(OperationsHub.GroupName(condominiumId))
            .SendAsync(
                "WhatsAppMessageStatusChanged",
                condominiumId,
                conversationId,
                messageId,
                cancellationToken);
}

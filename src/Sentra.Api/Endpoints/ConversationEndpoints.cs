using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Sentra.Api.Realtime;
using Sentra.Application.Abstractions;
using Sentra.Application.Integrations.WhatsApp;
using Sentra.Application.Security;
using Sentra.Contracts.WhatsApp;
using Sentra.Domain.Auditing;
using Sentra.Domain.Conversations;
using Sentra.Domain.Integrations;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/v1/conversations",
            ListConversationsAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsRead);

        endpoints.MapGet(
            "/api/v1/conversations/{conversationId:guid}/messages",
            ListMessagesAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsRead);

        endpoints.MapPost(
            "/api/v1/conversations/{conversationId:guid}/messages/text",
            SendTextAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsManage);

        return endpoints;
    }

    private static async Task<IResult> ListConversationsAsync(
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        var conversations = await db.Conversations
            .AsNoTracking()
            .Where(item => item.CondominiumId == condominiumId)
            .OrderByDescending(item => item.LastMessageAt)
            .Take(200)
            .Select(item => new ConversationSummaryResponse(
                item.Id,
                item.Channel.ToString(),
                item.ExternalParticipantId,
                item.DisplayName,
                item.ResidentId,
                item.UnitId,
                item.Status.ToString(),
                item.LastMessageAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(conversations);
    }

    private static async Task<IResult> ListMessagesAsync(
        Guid conversationId,
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        var conversationExists = await db.Conversations.AnyAsync(
            item =>
                item.Id == conversationId &&
                item.CondominiumId == condominiumId,
            cancellationToken);

        if (!conversationExists)
        {
            return Results.NotFound();
        }

        var messages = await db.Messages
            .AsNoTracking()
            .Where(item => item.ConversationId == conversationId)
            .OrderByDescending(item => item.OccurredAt)
            .Take(200)
            .OrderBy(item => item.OccurredAt)
            .Select(item => new ConversationMessageResponse(
                item.Id,
                item.Direction.ToString(),
                item.ContentKind.ToString(),
                item.Text,
                item.ExternalMessageId,
                item.ClientRequestId,
                item.OccurredAt,
                item.DeliveryStatus.ToString(),
                item.DeliveryStatusAt,
                item.LastErrorCode))
            .ToListAsync(cancellationToken);

        return Results.Ok(messages);
    }

    private static async Task<IResult> SendTextAsync(
        Guid conversationId,
        SendWhatsAppTextRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        IHubContext<OperationsHub> hub,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        if (request.ClientRequestId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Text))
        {
            return Results.BadRequest(
                new { error = "ClientRequestId e texto são obrigatórios." });
        }

        var existingRequest = await (
            from existingMessage in db.Messages.AsNoTracking()
            join existingConversation in db.Conversations.AsNoTracking()
                on existingMessage.ConversationId equals existingConversation.Id
            where existingMessage.ClientRequestId == request.ClientRequestId
                  && existingConversation.CondominiumId == condominiumId
            select existingMessage)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingRequest is not null)
        {
            return Results.Ok(ToResponse(existingRequest));
        }

        var conversation = await db.Conversations.SingleOrDefaultAsync(
            item =>
                item.Id == conversationId &&
                item.CondominiumId == condominiumId &&
                item.Channel == ConversationChannel.WhatsApp,
            cancellationToken);

        if (conversation is null)
        {
            return Results.NotFound();
        }

        var integrationConnected = await db.Integrations.AnyAsync(
            item =>
                item.CondominiumId == condominiumId &&
                item.Kind == IntegrationKind.WhatsApp &&
                item.Status == IntegrationStatus.Connected,
            cancellationToken);

        if (!integrationConnected)
        {
            return Results.Conflict(
                new { error = "WhatsApp não está validado para este condomínio." });
        }

        var now = clock.UtcNow;
        var message = Message.CreateOutboundPending(
            conversation.Id,
            request.ClientRequestId,
            request.Text,
            now);

        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var sent = await client.SendTextAsync(
                "+" + conversation.ExternalParticipantId.TrimStart('+'),
                request.Text,
                cancellationToken);

            var acceptedAt = clock.UtcNow;
            message.MarkAccepted(sent.MessageId, acceptedAt);
            conversation.RegisterMessage(acceptedAt);

            // Persist the provider message id before reconciling statuses.
            // If a status webhook raced ahead of this HTTP response, its
            // durable status event can now be applied without losing state.
            await db.SaveChangesAsync(cancellationToken);

            var priorStatuses = await db.MessageStatusEvents
                .AsNoTracking()
                .Where(item => item.ExternalMessageId == sent.MessageId)
                .OrderBy(item => item.OccurredAt)
                .ToListAsync(cancellationToken);

            foreach (var status in priorStatuses)
            {
                message.ApplyDeliveryStatus(
                    status.Status,
                    status.OccurredAt,
                    status.ErrorCode);
            }

            db.AuditEvents.Add(
                new AuditEvent(
                    "conversation.whatsapp.message.sent",
                    nameof(Message),
                    message.Id.ToString(),
                    "accepted",
                    acceptedAt,
                    user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue("sub"),
                    httpContext.Items["X-Correlation-ID"]?.ToString()));

            await db.SaveChangesAsync(cancellationToken);

            await hub.Clients
                .Group(OperationsHub.GroupName(condominiumId))
                .SendAsync(
                    "ConversationChanged",
                    new
                    {
                        conversationId = conversation.Id,
                        messageId = message.Id,
                        state = message.DeliveryStatus.ToString()
                    },
                    cancellationToken);

            return Results.Ok(ToResponse(message));
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            message.MarkSendFailure("meta_timeout", clock.UtcNow);
            await db.SaveChangesAsync(CancellationToken.None);

            return Results.Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Envio não confirmado",
                detail:
                    "O SENTRA não repetirá automaticamente esta operação porque não pode confirmar se a Meta recebeu a mensagem.");
        }
        catch (HttpRequestException)
        {
            message.MarkSendFailure("meta_send_failed", clock.UtcNow);
            await db.SaveChangesAsync(CancellationToken.None);

            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Envio não confirmado",
                detail:
                    "A API da Meta não confirmou o envio. O SENTRA não fará retry automático de uma operação não confirmada.");
        }
    }

    private static ConversationMessageResponse ToResponse(Message item)
        => new(
            item.Id,
            item.Direction.ToString(),
            item.ContentKind.ToString(),
            item.Text,
            item.ExternalMessageId,
            item.ClientRequestId,
            item.OccurredAt,
            item.DeliveryStatus.ToString(),
            item.DeliveryStatusAt,
            item.LastErrorCode);

    private static bool TryGetCondominiumId(
        ClaimsPrincipal user,
        out Guid condominiumId)
        => Guid.TryParse(
            user.FindFirst("condominium_id")?.Value,
            out condominiumId);
}

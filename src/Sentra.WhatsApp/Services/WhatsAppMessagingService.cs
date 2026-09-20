using Microsoft.EntityFrameworkCore;
using Sentra.Application.Abstractions;
using Sentra.Contracts.Common;
using Sentra.Contracts.WhatsApp;
using Sentra.Domain.Auditing;
using Sentra.Domain.WhatsApp;
using Sentra.Infrastructure.Persistence;
using Sentra.WhatsApp.Configuration;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp.Services;

public sealed class WhatsAppMessagingService(
    SentraDbContext dbContext,
    IMetaWhatsAppClient meta,
    WhatsAppOptions options,
    IClock clock) : IWhatsAppMessagingService
{
    public async Task<PagedResponse<WhatsAppConversationResponse>> ListConversationsAsync(
        Guid condominiumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Page(page, pageSize);
        var now = clock.UtcNow;
        var query = dbContext.WhatsAppConversations.AsNoTracking()
            .Where(x => x.CondominiumId == condominiumId);
        var total = await query.CountAsync(cancellationToken);
        var entities = await query.OrderByDescending(x => x.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new(
            entities.Select(x => new WhatsAppConversationResponse(
                x.Id,
                x.CondominiumId,
                x.ExternalParticipantId,
                x.ResidentId,
                x.DisplayName,
                x.LastInboundAt,
                x.LastMessageAt,
                x.IsInsideCustomerServiceWindow(now))).ToArray(),
            page,
            pageSize,
            total);
    }

    public async Task<PagedResponse<WhatsAppMessageResponse>> ListMessagesAsync(
        Guid condominiumId,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = Page(page, pageSize);
        var query = dbContext.WhatsAppMessages.AsNoTracking()
            .Where(x => x.CondominiumId == condominiumId && x.ConversationId == conversationId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.MessageTimestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WhatsAppMessageResponse(
                x.Id,
                x.ConversationId,
                x.ExternalMessageId,
                (int)x.Direction,
                (int)x.Type,
                x.Text,
                x.MessageTimestamp,
                (int)x.DeliveryStatus,
                x.DeliveryStatusAt,
                x.ErrorTitle))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }

    public Task<WhatsAppSendResult> SendTextAsync(
        Guid condominiumId,
        SendWhatsAppTextRequest request,
        string actor,
        CancellationToken cancellationToken)
        => SendFreeFormAsync(
            condominiumId,
            request.To,
            WhatsAppMessageType.Text,
            request.Text,
            actor,
            (to, ct) => meta.SendTextAsync(to, request.Text, request.ReplyToMessageId, ct),
            cancellationToken);

    public Task<WhatsAppSendResult> SendButtonsAsync(
        Guid condominiumId,
        SendWhatsAppButtonsRequest request,
        string actor,
        CancellationToken cancellationToken)
        => SendFreeFormAsync(
            condominiumId,
            request.To,
            WhatsAppMessageType.Interactive,
            request.Body,
            actor,
            (to, ct) => meta.SendButtonsAsync(to, request.Body, request.Buttons, ct),
            cancellationToken);

    public Task<WhatsAppSendResult> SendListAsync(
        Guid condominiumId,
        SendWhatsAppListRequest request,
        string actor,
        CancellationToken cancellationToken)
        => SendFreeFormAsync(
            condominiumId,
            request.To,
            WhatsAppMessageType.Interactive,
            request.Body,
            actor,
            (to, ct) => meta.SendListAsync(to, request.Body, request.ButtonText, request.Sections, ct),
            cancellationToken);

    public async Task<WhatsAppSendResult> SendTemplateAsync(
        Guid condominiumId,
        SendWhatsAppTemplateRequest request,
        string actor,
        CancellationToken cancellationToken)
    {
        await RequireIntegrationAsync(condominiumId, cancellationToken);
        var participant = NormalizeParticipant(request.To);
        var conversation = await GetOrCreateConversationAsync(condominiumId, participant, actor, cancellationToken);
        var sent = await meta.SendTemplateAsync(
            participant,
            request.TemplateName,
            request.LanguageCode,
            request.BodyParameters,
            cancellationToken);
        return await PersistOutboundAsync(
            condominiumId,
            conversation,
            WhatsAppMessageType.Template,
            $"Template: {request.TemplateName}",
            sent.MessageId,
            actor,
            cancellationToken);
    }

    public Task<WhatsAppSendResult> SendMediaAsync(
        Guid condominiumId,
        SendWhatsAppMediaRequest request,
        string actor,
        CancellationToken cancellationToken)
        => SendFreeFormAsync(
            condominiumId,
            request.To,
            MediaType(request.MediaType),
            request.Caption,
            actor,
            (to, ct) => meta.SendMediaAsync(to, request.MediaId, request.MediaType, request.Caption, request.FileName, ct),
            cancellationToken);

    public Task<WhatsAppSendResult> SendFlowAsync(
        Guid condominiumId,
        SendWhatsAppFlowRequest request,
        string actor,
        CancellationToken cancellationToken)
        => SendFreeFormAsync(
            condominiumId,
            request.To,
            WhatsAppMessageType.Flow,
            request.Body,
            actor,
            (_, ct) => meta.SendFlowAsync(request with { To = NormalizeParticipant(request.To) }, ct),
            cancellationToken);

    public Task<string> UploadMediaAsync(
        Stream stream,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken)
        => meta.UploadMediaAsync(stream, fileName, mimeType, cancellationToken);

    private async Task<WhatsAppSendResult> SendFreeFormAsync(
        Guid condominiumId,
        string to,
        WhatsAppMessageType type,
        string? text,
        string actor,
        Func<string, CancellationToken, Task<MetaSendResult>> send,
        CancellationToken cancellationToken)
    {
        await RequireIntegrationAsync(condominiumId, cancellationToken);
        var participant = NormalizeParticipant(to);
        var conversation = await dbContext.WhatsAppConversations
            .SingleOrDefaultAsync(
                x => x.CondominiumId == condominiumId && x.ExternalParticipantId == participant,
                cancellationToken);

        if (conversation is null || !conversation.IsInsideCustomerServiceWindow(clock.UtcNow))
            throw new CustomerServiceWindowClosedException();

        var sent = await send(participant, cancellationToken);
        return await PersistOutboundAsync(
            condominiumId,
            conversation,
            type,
            text,
            sent.MessageId,
            actor,
            cancellationToken);
    }

    private async Task<WhatsAppSendResult> PersistOutboundAsync(
        Guid condominiumId,
        WhatsAppConversation conversation,
        WhatsAppMessageType type,
        string? text,
        string externalMessageId,
        string actor,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var message = new WhatsAppMessage(
            condominiumId,
            conversation.Id,
            WhatsAppMessageDirection.Outbound,
            type,
            now,
            externalMessageId,
            text);

        dbContext.WhatsAppMessages.Add(message);
        conversation.RecordOutbound(now);
        dbContext.AuditEvents.Add(new AuditEvent(
            "whatsapp.message.sent",
            nameof(WhatsAppMessage),
            message.Id.ToString(),
            "success",
            now,
            actor));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(message.Id, externalMessageId);
    }

    private async Task<WhatsAppConversation> GetOrCreateConversationAsync(
        Guid condominiumId,
        string participant,
        string actor,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.WhatsAppConversations.SingleOrDefaultAsync(
            x => x.CondominiumId == condominiumId && x.ExternalParticipantId == participant,
            cancellationToken);
        if (conversation is not null) return conversation;

        conversation = new WhatsAppConversation(condominiumId, participant, actor, clock.UtcNow);
        dbContext.WhatsAppConversations.Add(conversation);
        return conversation;
    }

    private async Task RequireIntegrationAsync(Guid condominiumId, CancellationToken cancellationToken)
    {
        var enabled = await dbContext.WhatsAppIntegrations.AsNoTracking()
            .AnyAsync(
                x => x.CondominiumId == condominiumId
                    && x.IsEnabled
                    && x.PhoneNumberId == options.PhoneNumberId
                    && x.WabaId == options.WabaId,
                cancellationToken);
        if (!enabled)
            throw new InvalidOperationException("WhatsApp não está validado e ativo para este condomínio.");
    }

    private static string NormalizeParticipant(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length is < 8 or > 15)
            throw new ArgumentException("Número WhatsApp inválido.", nameof(value));
        return digits;
    }

    private static WhatsAppMessageType MediaType(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "image" => WhatsAppMessageType.Image,
            "audio" => WhatsAppMessageType.Audio,
            "document" => WhatsAppMessageType.Document,
            "video" => WhatsAppMessageType.Video,
            "sticker" => WhatsAppMessageType.Sticker,
            _ => throw new ArgumentException("Tipo de mídia não suportado.", nameof(value))
        };

    private static (int Page, int PageSize) Page(int page, int pageSize)
        => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
}

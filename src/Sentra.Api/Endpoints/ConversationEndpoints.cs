using System.Security.Cryptography;
using System.Net;
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

        endpoints.MapGet(
            "/api/v1/conversations/{conversationId:guid}/messages/{messageId:guid}/attachments",
            ListAttachmentsAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsRead);

        endpoints.MapGet(
            "/api/v1/conversations/{conversationId:guid}/attachments/{attachmentId:guid}/content",
            DownloadAttachmentAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsRead);

        endpoints.MapPost(
            "/api/v1/conversations/{conversationId:guid}/messages/{messageId:guid}/read",
            MarkReadAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsRead);

        endpoints.MapPost(
            "/api/v1/conversations/{conversationId:guid}/messages/text",
            SendTextAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsManage);

        endpoints.MapPost(
            "/api/v1/conversations/{conversationId:guid}/messages/template",
            SendTemplateAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsManage);

        endpoints.MapPost(
            "/api/v1/conversations/{conversationId:guid}/messages/buttons",
            SendButtonsAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsManage);

        endpoints.MapPost(
            "/api/v1/conversations/{conversationId:guid}/messages/list",
            SendListAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsManage);

        endpoints.MapPost(
            "/api/v1/conversations/{conversationId:guid}/messages/flow",
            SendFlowAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsManage);

        endpoints.MapPost(
            "/api/v1/conversations/{conversationId:guid}/messages/media",
            SendMediaAsync)
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

        if (!await ConversationExistsAsync(
                db,
                condominiumId,
                conversationId,
                cancellationToken))
        {
            return Results.NotFound();
        }

        var messages = await db.Messages
            .AsNoTracking()
            .Where(item => item.ConversationId == conversationId)
            .OrderByDescending(item => item.OccurredAt)
            .Take(200)
            .OrderBy(item => item.OccurredAt)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);

        return Results.Ok(messages);
    }

    private static async Task<IResult> ListAttachmentsAsync(
        Guid conversationId,
        Guid messageId,
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        var messageExists = await (
            from message in db.Messages.AsNoTracking()
            join conversation in db.Conversations.AsNoTracking()
                on message.ConversationId equals conversation.Id
            where conversation.Id == conversationId
                  && conversation.CondominiumId == condominiumId
                  && message.Id == messageId
            select message.Id)
            .AnyAsync(cancellationToken);

        if (!messageExists)
        {
            return Results.NotFound();
        }

        var attachments = await db.Attachments
            .AsNoTracking()
            .Where(item => item.MessageId == messageId)
            .Select(item => new ConversationAttachmentResponse(
                item.Id,
                item.MessageId,
                item.MimeType,
                item.FileName,
                item.StorageStatus.ToString()))
            .ToListAsync(cancellationToken);

        return Results.Ok(attachments);
    }

    private static async Task DownloadAttachmentAsync(
        Guid conversationId,
        Guid attachmentId,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var attachment = await (
            from candidate in db.Attachments.AsNoTracking()
            join message in db.Messages.AsNoTracking()
                on candidate.MessageId equals message.Id
            join conversation in db.Conversations.AsNoTracking()
                on message.ConversationId equals conversation.Id
            where conversation.Id == conversationId
                  && conversation.CondominiumId == condominiumId
                  && candidate.Id == attachmentId
            select candidate)
            .SingleOrDefaultAsync(cancellationToken);

        if (attachment is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        try
        {
            var media = await client.GetMediaInfoAsync(
                attachment.ExternalMediaId,
                cancellationToken);

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType =
                media.MimeType ?? attachment.MimeType ?? "application/octet-stream";

            if (media.FileSize is > 0)
            {
                httpContext.Response.ContentLength = media.FileSize;
            }

            var safeFileName = SanitizeFileName(
                attachment.FileName ?? $"whatsapp-{attachment.Id:N}");

            httpContext.Response.Headers.ContentDisposition =
                $"inline; filename=\"{safeFileName}\"";

            await client.DownloadMediaAsync(
                attachment.ExternalMediaId,
                httpContext.Response.Body,
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = exception.StatusCode switch
                {
                    HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        StatusCodes.Status502BadGateway,
                    _ => StatusCodes.Status502BadGateway
                };
            }
        }
    }

    private static async Task<IResult> MarkReadAsync(
        Guid conversationId,
        Guid messageId,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        var message = await (
            from candidate in db.Messages
            join conversation in db.Conversations
                on candidate.ConversationId equals conversation.Id
            where conversation.Id == conversationId
                  && conversation.CondominiumId == condominiumId
                  && candidate.Id == messageId
                  && candidate.Direction == MessageDirection.Inbound
            select candidate)
            .SingleOrDefaultAsync(cancellationToken);

        if (message is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(message.ExternalMessageId))
        {
            return Results.Conflict(
                new { error = "Mensagem não possui identificador externo para confirmação de leitura." });
        }

        try
        {
            await client.MarkMessageReadAsync(
                message.ExternalMessageId,
                cancellationToken);

            db.AuditEvents.Add(
                new AuditEvent(
                    "conversation.whatsapp.message.read",
                    nameof(Message),
                    message.Id.ToString(),
                    "success",
                    clock.UtcNow,
                    Actor(user),
                    Correlation(httpContext)));

            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }
        catch (HttpRequestException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Não foi possível confirmar a leitura",
                detail: "A API oficial da Meta não confirmou a operação.");
        }
    }

    private static Task<IResult> SendTextAsync(
        Guid conversationId,
        SendWhatsAppTextRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        IHubContext<OperationsHub> hub,
        CancellationToken cancellationToken)
        => SendOutboundAsync(
            conversationId,
            request.ClientRequestId,
            MessageContentKind.Text,
            request.Text,
            user,
            httpContext,
            db,
            client,
            clock,
            hub,
            (recipient, token) =>
                client.SendTextAsync(recipient, request.Text, token),
            cancellationToken);

    private static async Task<IResult> SendTemplateAsync(
        Guid conversationId,
        SendWhatsAppTemplateRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        IHubContext<OperationsHub> hub,
        CancellationToken cancellationToken)
    {
        var templates = await client.GetTemplatesAsync(cancellationToken);
        var approved = templates.Any(item =>
            string.Equals(item.Name, request.TemplateName, StringComparison.Ordinal) &&
            string.Equals(item.Language, request.LanguageCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Status, "APPROVED", StringComparison.OrdinalIgnoreCase));

        if (!approved)
        {
            return Results.BadRequest(
                new { error = "Template/língua não está aprovado na WABA configurada." });
        }

        return await SendOutboundAsync(
            conversationId,
            request.ClientRequestId,
            MessageContentKind.Template,
            $"Template: {request.TemplateName}",
            user,
            httpContext,
            db,
            client,
            clock,
            hub,
            (recipient, token) =>
                client.SendTemplateAsync(
                    recipient,
                    request.TemplateName,
                    request.LanguageCode,
                    request.BodyParameters,
                    token),
            cancellationToken);
    }

    private static Task<IResult> SendButtonsAsync(
        Guid conversationId,
        SendWhatsAppReplyButtonsRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        IHubContext<OperationsHub> hub,
        CancellationToken cancellationToken)
    {
        var buttons = request.Buttons
            .Select(item => new WhatsAppInteractiveButton(item.Id, item.Title))
            .ToArray();

        return SendOutboundAsync(
            conversationId,
            request.ClientRequestId,
            MessageContentKind.Interactive,
            request.Body,
            user,
            httpContext,
            db,
            client,
            clock,
            hub,
            (recipient, token) =>
                client.SendReplyButtonsAsync(
                    recipient,
                    request.Body,
                    buttons,
                    token),
            cancellationToken);
    }

    private static Task<IResult> SendListAsync(
        Guid conversationId,
        SendWhatsAppListRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        IHubContext<OperationsHub> hub,
        CancellationToken cancellationToken)
    {
        var sections = request.Sections
            .Select(section => new WhatsAppListSection(
                section.Title,
                section.Rows.Select(row => new WhatsAppListRow(
                    row.Id,
                    row.Title,
                    row.Description)).ToArray()))
            .ToArray();

        return SendOutboundAsync(
            conversationId,
            request.ClientRequestId,
            MessageContentKind.Interactive,
            request.Body,
            user,
            httpContext,
            db,
            client,
            clock,
            hub,
            (recipient, token) =>
                client.SendListAsync(
                    recipient,
                    request.Body,
                    request.ButtonText,
                    sections,
                    token),
            cancellationToken);
    }

    private static async Task<IResult> SendFlowAsync(
        Guid conversationId,
        SendWhatsAppFlowRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        IHubContext<OperationsHub> hub,
        CancellationToken cancellationToken)
    {
        var flows = await client.GetFlowsAsync(cancellationToken);
        var published = flows.Any(item =>
            string.Equals(item.Id, request.FlowId, StringComparison.Ordinal) &&
            string.Equals(item.Status, "PUBLISHED", StringComparison.OrdinalIgnoreCase));

        if (!published)
        {
            return Results.BadRequest(
                new { error = "Flow não está publicado na WABA configurada." });
        }

        var flowToken = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(16));

        return await SendOutboundAsync(
            conversationId,
            request.ClientRequestId,
            MessageContentKind.Interactive,
            request.Body,
            user,
            httpContext,
            db,
            client,
            clock,
            hub,
            (recipient, token) =>
                client.SendFlowAsync(
                    recipient,
                    request.FlowId,
                    flowToken,
                    request.CallToAction,
                    request.Body,
                    request.Screen,
                    request.Data,
                    token),
            cancellationToken);
    }

    private static async Task<IResult> SendMediaAsync(
        Guid conversationId,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        IHubContext<OperationsHub> hub,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.HasFormContentType)
        {
            return Results.BadRequest(
                new { error = "Envio de mídia exige multipart/form-data." });
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);

        if (!Guid.TryParse(form["clientRequestId"].ToString(), out var clientRequestId) ||
            clientRequestId == Guid.Empty)
        {
            return Results.BadRequest(
                new { error = "clientRequestId inválido." });
        }

        if (!Enum.TryParse<WhatsAppMediaKind>(
                form["kind"].ToString(),
                ignoreCase: true,
                out var mediaKind))
        {
            return Results.BadRequest(
                new { error = "kind deve ser Image, Audio, Video ou Document." });
        }

        if (form.Files.Count != 1)
        {
            return Results.BadRequest(
                new { error = "Envie exatamente um arquivo." });
        }

        var file = form.Files[0];

        if (file.Length <= 0 ||
            string.IsNullOrWhiteSpace(file.ContentType))
        {
            return Results.BadRequest(
                new { error = "Arquivo vazio ou sem Content-Type." });
        }

        var existing = await FindExistingRequestAsync(
            GetCondominiumIdOrEmpty(user),
            clientRequestId,
            db,
            cancellationToken);

        if (existing is not null)
        {
            return existing.ConversationId == conversationId
                ? Results.Ok(ToResponse(existing))
                : Results.Conflict(
                    new { error = "ClientRequestId já pertence a outra conversa." });
        }

        WhatsAppMediaUploadResult uploaded;

        try
        {
            await using var stream = file.OpenReadStream();
            uploaded = await client.UploadMediaAsync(
                stream,
                file.FileName,
                file.ContentType,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Falha no upload da mídia",
                detail: "A API oficial da Meta não confirmou o upload.");
        }

        var caption = form["caption"].ToString();
        var summary = string.IsNullOrWhiteSpace(caption)
            ? file.FileName
            : caption;

        var kind = mediaKind switch
        {
            WhatsAppMediaKind.Image => MessageContentKind.Image,
            WhatsAppMediaKind.Audio => MessageContentKind.Audio,
            WhatsAppMediaKind.Video => MessageContentKind.Video,
            WhatsAppMediaKind.Document => MessageContentKind.Document,
            _ => MessageContentKind.Unknown
        };

        var result = await SendOutboundAsync(
            conversationId,
            clientRequestId,
            kind,
            summary,
            user,
            httpContext,
            db,
            client,
            clock,
            hub,
            (recipient, token) =>
                client.SendMediaAsync(
                    recipient,
                    mediaKind,
                    uploaded.MediaId,
                    string.IsNullOrWhiteSpace(caption) ? null : caption,
                    file.FileName,
                    token),
            cancellationToken);

        return result;
    }

    private static async Task<IResult> SendOutboundAsync(
        Guid conversationId,
        Guid clientRequestId,
        MessageContentKind kind,
        string summary,
        ClaimsPrincipal user,
        HttpContext httpContext,
        SentraDbContext db,
        IWhatsAppClient client,
        IClock clock,
        IHubContext<OperationsHub> hub,
        Func<string, CancellationToken, Task<WhatsAppSendResult>> sendAction,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        if (clientRequestId == Guid.Empty ||
            string.IsNullOrWhiteSpace(summary))
        {
            return Results.BadRequest(
                new { error = "ClientRequestId e conteúdo são obrigatórios." });
        }

        var existingRequest = await FindExistingRequestAsync(
            condominiumId,
            clientRequestId,
            db,
            cancellationToken);

        if (existingRequest is not null)
        {
            return existingRequest.ConversationId == conversationId
                ? Results.Ok(ToResponse(existingRequest))
                : Results.Conflict(
                    new
                    {
                        error =
                            "ClientRequestId já foi usado em outra conversa deste condomínio."
                    });
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

        if (!await db.Integrations.AnyAsync(
                item =>
                    item.CondominiumId == condominiumId &&
                    item.Kind == IntegrationKind.WhatsApp &&
                    item.Status == IntegrationStatus.Connected,
                cancellationToken))
        {
            return Results.Conflict(
                new { error = "WhatsApp não está validado para este condomínio." });
        }

        var now = clock.UtcNow;
        var message = Message.CreateOutboundPending(
            conversation.Id,
            clientRequestId,
            kind,
            summary,
            now);

        db.Messages.Add(message);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (PostgresErrorClassifier.IsUniqueViolation(exception))
        {
            db.Entry(message).State = EntityState.Detached;

            var concurrentRequest = await FindExistingRequestAsync(
                condominiumId,
                clientRequestId,
                db,
                cancellationToken);

            if (concurrentRequest is not null)
            {
                return concurrentRequest.ConversationId == conversationId
                    ? Results.Ok(ToResponse(concurrentRequest))
                    : Results.Conflict(
                        new { error = "ClientRequestId já foi usado em outra conversa." });
            }

            return Results.Conflict(
                new { error = "ClientRequestId já está em uso." });
        }

        try
        {
            var recipient =
                "+" + conversation.ExternalParticipantId.TrimStart('+');

            var sent = await sendAction(recipient, cancellationToken);

            var acceptedAt = clock.UtcNow;
            message.MarkAccepted(sent.MessageId, acceptedAt);
            conversation.RegisterMessage(acceptedAt);

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
                CreateSendAudit(
                    user,
                    httpContext,
                    message,
                    "accepted",
                    acceptedAt));

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
        catch (ArgumentException exception)
        {
            var failedAt = clock.UtcNow;
            message.MarkSendFailure("invalid_message_payload", failedAt);
            await db.SaveChangesAsync(CancellationToken.None);

            return Results.BadRequest(
                new { error = exception.Message });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            var uncertainAt = clock.UtcNow;
            message.MarkSendUncertain("meta_timeout", uncertainAt);
            db.AuditEvents.Add(
                CreateSendAudit(
                    user,
                    httpContext,
                    message,
                    "uncertain",
                    uncertainAt));
            await db.SaveChangesAsync(CancellationToken.None);

            return Results.Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Envio não confirmado",
                detail:
                    "Não foi possível determinar se a Meta recebeu a mensagem. O SENTRA não repetirá automaticamente esta operação.");
        }
        catch (HttpRequestException exception)
        {
            var failureAt = clock.UtcNow;
            var confirmedRejection = exception.StatusCode is not null;

            if (confirmedRejection)
            {
                message.MarkSendFailure(
                    $"meta_http_{(int)exception.StatusCode!.Value}",
                    failureAt);
            }
            else
            {
                message.MarkSendUncertain(
                    "meta_transport_uncertain",
                    failureAt);
            }

            db.AuditEvents.Add(
                CreateSendAudit(
                    user,
                    httpContext,
                    message,
                    confirmedRejection ? "failed" : "uncertain",
                    failureAt));

            await db.SaveChangesAsync(CancellationToken.None);

            return Results.Problem(
                statusCode: confirmedRejection
                    ? StatusCodes.Status502BadGateway
                    : StatusCodes.Status503ServiceUnavailable,
                title: confirmedRejection
                    ? "Envio rejeitado pela integração"
                    : "Envio não confirmado",
                detail: confirmedRejection
                    ? "A API oficial da Meta rejeitou a solicitação."
                    : "A conexão terminou sem confirmação. O SENTRA não repetirá automaticamente esta operação.");
        }
    }

    private static Task<Message?> FindExistingRequestAsync(
        Guid condominiumId,
        Guid clientRequestId,
        SentraDbContext db,
        CancellationToken cancellationToken)
        => (
            from existingMessage in db.Messages.AsNoTracking()
            join existingConversation in db.Conversations.AsNoTracking()
                on existingMessage.ConversationId equals existingConversation.Id
            where existingMessage.ClientRequestId == clientRequestId
                  && existingConversation.CondominiumId == condominiumId
            select existingMessage)
            .SingleOrDefaultAsync(cancellationToken);

    private static Task<bool> ConversationExistsAsync(
        SentraDbContext db,
        Guid condominiumId,
        Guid conversationId,
        CancellationToken cancellationToken)
        => db.Conversations.AsNoTracking().AnyAsync(
            item =>
                item.Id == conversationId &&
                item.CondominiumId == condominiumId,
            cancellationToken);

    private static AuditEvent CreateSendAudit(
        ClaimsPrincipal user,
        HttpContext httpContext,
        Message message,
        string outcome,
        DateTimeOffset timestamp)
        => new(
            "conversation.whatsapp.message.sent",
            nameof(Message),
            message.Id.ToString(),
            outcome,
            timestamp,
            Actor(user),
            Correlation(httpContext));

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

    private static string Actor(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? "unknown";

    private static string? Correlation(HttpContext context)
        => context.Items["X-Correlation-ID"]?.ToString();

    private static Guid GetCondominiumIdOrEmpty(ClaimsPrincipal user)
        => TryGetCondominiumId(user, out var condominiumId)
            ? condominiumId
            : Guid.Empty;

    private static bool TryGetCondominiumId(
        ClaimsPrincipal user,
        out Guid condominiumId)
        => Guid.TryParse(
            user.FindFirst("condominium_id")?.Value,
            out condominiumId);

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(
            value.Where(character =>
                !invalid.Contains(character) &&
                character is not '\r' and not '\n' and not '"')
                .ToArray());

        return string.IsNullOrWhiteSpace(sanitized)
            ? "whatsapp-media"
            : sanitized;
    }
}

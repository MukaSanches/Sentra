using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Sentra.Contracts.WhatsApp;
using Sentra.Domain.Access;
using Sentra.Infrastructure.Persistence;
using Sentra.WhatsApp.Meta;
using Sentra.WhatsApp.Services;

namespace Sentra.Api.Endpoints;

public static class WhatsAppEndpoints
{
    private const int MaxWebhookBytes = 1024 * 1024;

    public static IEndpointRouteBuilder MapWhatsAppEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/webhooks/whatsapp", VerifyWebhook)
            .AllowAnonymous();

        endpoints.MapPost("/api/webhooks/whatsapp", ReceiveWebhook)
            .AllowAnonymous();

        var group = endpoints.MapGroup("/api/condominiums/{condominiumId:guid}/whatsapp");

        group.MapGet("/status", async (
            Guid condominiumId,
            IWhatsAppIntegrationService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.GetStatusAsync(condominiumId, cancellationToken)))
            .RequireAuthorization(PermissionCodes.IntegrationsManage);

        group.MapPost("/configure", async (
            Guid condominiumId,
            HttpContext context,
            IWhatsAppIntegrationService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.ConfigureAsync(
                    condominiumId,
                    Actor(context),
                    cancellationToken)))
            .RequireAuthorization(PermissionCodes.IntegrationsManage);

        group.MapGet("/templates", async (
            IMetaWhatsAppClient meta,
            CancellationToken cancellationToken) =>
        {
            var templates = await meta.GetTemplatesAsync(cancellationToken);
            return Results.Ok(templates.Select(x => new WhatsAppTemplateResponse(
                x.Id, x.Name, x.Language, x.Status, x.Category)));
        }).RequireAuthorization(PermissionCodes.ConversationsSend);

        group.MapGet("/flows", async (
            IMetaWhatsAppClient meta,
            CancellationToken cancellationToken) =>
        {
            var flows = await meta.GetFlowsAsync(cancellationToken);
            return Results.Ok(flows.Select(x => new WhatsAppFlowResponse(x.Id, x.Name, x.Status)));
        }).RequireAuthorization(PermissionCodes.ConversationsSend);

        group.MapGet("/conversations", async (
            Guid condominiumId,
            int page,
            int pageSize,
            IWhatsAppMessagingService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.ListConversationsAsync(
                    condominiumId,
                    page,
                    pageSize,
                    cancellationToken)))
            .RequireAuthorization(PermissionCodes.ConversationsRead);

        group.MapGet("/conversations/{conversationId:guid}/messages", async (
            Guid condominiumId,
            Guid conversationId,
            int page,
            int pageSize,
            IWhatsAppMessagingService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.ListMessagesAsync(
                    condominiumId,
                    conversationId,
                    page,
                    pageSize,
                    cancellationToken)))
            .RequireAuthorization(PermissionCodes.ConversationsRead);

        group.MapGet(
            "/conversations/{conversationId:guid}/messages/{messageId:guid}/attachments/{attachmentId:guid}",
            GetAttachment)
            .RequireAuthorization(PermissionCodes.ConversationsRead);

        group.MapPost("/send/text", SendText)
            .RequireAuthorization(PermissionCodes.ConversationsSend);

        group.MapPost("/send/buttons", SendButtons)
            .RequireAuthorization(PermissionCodes.ConversationsSend);

        group.MapPost("/send/list", SendList)
            .RequireAuthorization(PermissionCodes.ConversationsSend);

        group.MapPost("/send/template", SendTemplate)
            .RequireAuthorization(PermissionCodes.ConversationsSend);

        group.MapPost("/send/media", SendMedia)
            .RequireAuthorization(PermissionCodes.ConversationsSend);

        group.MapPost("/send/flow", SendFlow)
            .RequireAuthorization(PermissionCodes.ConversationsSend);

        group.MapPost("/media", UploadMedia)
            .DisableAntiforgery()
            .RequireAuthorization(PermissionCodes.ConversationsSend);

        return endpoints;
    }

    private static IResult VerifyWebhook(
        HttpRequest request,
        IWhatsAppWebhookIngressService service)
    {
        var mode = request.Query["hub.mode"].ToString();
        var token = request.Query["hub.verify_token"].ToString();
        var challenge = request.Query["hub.challenge"].ToString();

        return service.TryVerifyChallenge(mode, token, challenge, out var response)
            ? Results.Text(response, "text/plain")
            : Results.Unauthorized();
    }

    private static async Task<IResult> ReceiveWebhook(
        HttpRequest request,
        IWhatsAppWebhookIngressService service,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength is > MaxWebhookBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        await using var memory = new MemoryStream();
        await request.Body.CopyToAsync(memory, cancellationToken);
        if (memory.Length > MaxWebhookBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        var signature = request.Headers["X-Hub-Signature-256"].ToString();
        var result = await service.AcceptAsync(memory.ToArray(), signature, cancellationToken);

        if (result.Accepted) return Results.Ok(new { accepted = true, duplicate = result.Duplicate });

        return result.Error == "INVALID_SIGNATURE"
            ? Results.Unauthorized()
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> SendText(
        Guid condominiumId,
        SendWhatsAppTextRequest request,
        HttpContext context,
        IWhatsAppMessagingService service,
        CancellationToken cancellationToken)
        => await Send(
            () => service.SendTextAsync(condominiumId, request, Actor(context), cancellationToken));

    private static async Task<IResult> SendButtons(
        Guid condominiumId,
        SendWhatsAppButtonsRequest request,
        HttpContext context,
        IWhatsAppMessagingService service,
        CancellationToken cancellationToken)
        => await Send(
            () => service.SendButtonsAsync(condominiumId, request, Actor(context), cancellationToken));

    private static async Task<IResult> SendList(
        Guid condominiumId,
        SendWhatsAppListRequest request,
        HttpContext context,
        IWhatsAppMessagingService service,
        CancellationToken cancellationToken)
        => await Send(
            () => service.SendListAsync(condominiumId, request, Actor(context), cancellationToken));

    private static async Task<IResult> SendTemplate(
        Guid condominiumId,
        SendWhatsAppTemplateRequest request,
        HttpContext context,
        IWhatsAppMessagingService service,
        CancellationToken cancellationToken)
        => await Send(
            () => service.SendTemplateAsync(condominiumId, request, Actor(context), cancellationToken));

    private static async Task<IResult> SendMedia(
        Guid condominiumId,
        SendWhatsAppMediaRequest request,
        HttpContext context,
        IWhatsAppMessagingService service,
        CancellationToken cancellationToken)
        => await Send(
            () => service.SendMediaAsync(condominiumId, request, Actor(context), cancellationToken));

    private static async Task<IResult> SendFlow(
        Guid condominiumId,
        SendWhatsAppFlowRequest request,
        HttpContext context,
        IWhatsAppMessagingService service,
        CancellationToken cancellationToken)
        => await Send(
            () => service.SendFlowAsync(condominiumId, request, Actor(context), cancellationToken));

    private static async Task<IResult> UploadMedia(
        HttpRequest request,
        IWhatsAppMessagingService service,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "multipart/form-data esperado." });

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "Arquivo não informado." });

        await using var stream = file.OpenReadStream();
        var id = await service.UploadMediaAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);
        return Results.Ok(new { mediaId = id });
    }

    private static async Task<IResult> GetAttachment(
        Guid condominiumId,
        Guid conversationId,
        Guid messageId,
        Guid attachmentId,
        SentraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var attachment = await (
            from item in dbContext.WhatsAppAttachments.AsNoTracking()
            join message in dbContext.WhatsAppMessages.AsNoTracking()
                on item.MessageId equals message.Id
            where item.Id == attachmentId
                && item.CondominiumId == condominiumId
                && message.Id == messageId
                && message.ConversationId == conversationId
            select item)
            .SingleOrDefaultAsync(cancellationToken);

        if (attachment?.Content is null)
            return Results.NotFound();

        return Results.File(
            attachment.Content,
            attachment.MimeType,
            attachment.FileName,
            enableRangeProcessing: true);
    }

    private static async Task<IResult> Send(Func<Task<WhatsAppSendResult>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (CustomerServiceWindowClosedException exception)
        {
            return Results.Conflict(new
            {
                code = "CUSTOMER_SERVICE_WINDOW_CLOSED",
                message = exception.Message
            });
        }
        catch (MetaWhatsAppException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Falha na WhatsApp Cloud API",
                detail: $"Meta retornou HTTP {exception.StatusCode}.");
        }
    }

    private static string Actor(HttpContext context)
        => context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Usuário autenticado sem subject.");
}

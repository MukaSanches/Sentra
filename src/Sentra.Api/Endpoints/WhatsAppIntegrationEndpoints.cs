using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Abstractions;
using Sentra.Application.Integrations.WhatsApp;
using Sentra.Application.Security;
using Sentra.Contracts.WhatsApp;
using Sentra.Domain.Auditing;
using Sentra.Domain.Integrations;
using Sentra.Infrastructure.Persistence;
using Sentra.WhatsApp;

namespace Sentra.Api.Endpoints;

public static class WhatsAppIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapWhatsAppIntegrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/v1/integrations/whatsapp/configuration",
            GetConfigurationAsync)
            .RequireAuthorization(PermissionCatalog.IntegrationsManage);

        endpoints.MapGet(
            "/api/v1/integrations/whatsapp/status",
            GetStatusAsync)
            .RequireAuthorization(PermissionCatalog.IntegrationsManage);

        endpoints.MapPost(
            "/api/v1/integrations/whatsapp/verify",
            VerifyAsync)
            .RequireAuthorization(PermissionCatalog.IntegrationsManage);

        endpoints.MapGet(
            "/api/v1/integrations/whatsapp/templates",
            GetTemplatesAsync)
            .RequireAuthorization(PermissionCatalog.IntegrationsManage);

        endpoints.MapGet(
            "/api/v1/integrations/whatsapp/flows",
            GetFlowsAsync)
            .RequireAuthorization(PermissionCatalog.IntegrationsManage);

        return endpoints;
    }

    private static Task<IResult> GetConfigurationAsync(
        IConfiguration configuration)
    {
        string[] required =
        [
            "META_GRAPH_VERSION",
            "META_WABA_ID",
            "META_PHONE_NUMBER_ID",
            "META_ACCESS_TOKEN",
            "META_VERIFY_TOKEN",
            "META_APP_SECRET"
        ];

        var issues = configuration.GetSentraWhatsAppConfigurationIssues();

        return Task.FromResult<IResult>(
            Results.Ok(
                new WhatsAppConfigurationStatusResponse(
                    issues.Count == 0,
                    required,
                    issues,
                    "/api/v1/integrations/whatsapp/webhook")));
    }

    private static async Task<IResult> GetStatusAsync(
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        var integration = await db.Integrations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.CondominiumId == condominiumId &&
                    item.Kind == IntegrationKind.WhatsApp,
                cancellationToken);

        if (integration is null)
        {
            return Results.Ok(
                new WhatsAppIntegrationStatusResponse(
                    "AGUARDANDO CONFIGURAÇÃO",
                    null,
                    null,
                    null,
                    null));
        }

        return Results.Ok(
            new WhatsAppIntegrationStatusResponse(
                integration.Status.ToString(),
                integration.ExternalResourceId,
                integration.DisplayName,
                integration.LastVerifiedAt,
                integration.LastErrorCode));
    }

    private static async Task<IResult> GetTemplatesAsync(
        ClaimsPrincipal user,
        SentraDbContext db,
        IWhatsAppClient client,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        if (!await IsConnectedAsync(db, condominiumId, cancellationToken))
        {
            return Results.Conflict(new { error = "WhatsApp ainda não foi validado." });
        }

        var templates = await client.GetTemplatesAsync(cancellationToken);

        return Results.Ok(
            templates.Select(item => new WhatsAppTemplateResponse(
                item.Id,
                item.Name,
                item.Language,
                item.Status,
                item.Category)));
    }

    private static async Task<IResult> GetFlowsAsync(
        ClaimsPrincipal user,
        SentraDbContext db,
        IWhatsAppClient client,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        if (!await IsConnectedAsync(db, condominiumId, cancellationToken))
        {
            return Results.Conflict(new { error = "WhatsApp ainda não foi validado." });
        }

        var flows = await client.GetFlowsAsync(cancellationToken);

        return Results.Ok(
            flows.Select(item => new WhatsAppFlowResponse(
                item.Id,
                item.Name,
                item.Status)));
    }

    private static async Task<IResult> VerifyAsync(
        ClaimsPrincipal user,
        HttpContext httpContext,
        IConfiguration configuration,
        IWhatsAppClient client,
        IClock clock,
        SentraDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetCondominiumId(user, out var condominiumId))
        {
            return Results.Unauthorized();
        }

        if (!configuration.IsSentraWhatsAppConfigured())
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "AGUARDANDO CONFIGURAÇÃO",
                detail: "Configure as credenciais oficiais da Meta antes de testar a integração.");
        }

        var existing = await db.Integrations.SingleOrDefaultAsync(
            item =>
                item.CondominiumId == condominiumId &&
                item.Kind == IntegrationKind.WhatsApp,
            cancellationToken);

        try
        {
            var info = await client.GetPhoneInfoAsync(cancellationToken);
            var wabaNumbers = await client.GetWabaPhoneNumbersAsync(cancellationToken);

            if (!wabaNumbers.Any(item =>
                    string.Equals(item.Id, info.Id, StringComparison.Ordinal)))
            {
                return Results.Conflict(
                    new
                    {
                        error =
                            "O Phone Number ID configurado não pertence à WABA configurada. Corrija META_PHONE_NUMBER_ID e META_WABA_ID antes de continuar."
                    });
            }

            if (existing is not null &&
                !string.Equals(
                    existing.ExternalResourceId,
                    info.Id,
                    StringComparison.Ordinal))
            {
                return Results.Conflict(
                    new
                    {
                        error =
                            "O condomínio já está vinculado a outro Phone Number ID. A troca exige operação administrativa explícita."
                    });
            }

            await client.SubscribeWabaAsync(cancellationToken);

            var templates = await client.GetTemplatesAsync(cancellationToken);
            var flows = await client.GetFlowsAsync(cancellationToken);

            var approvedTemplateCount = templates.Count(item =>
                string.Equals(item.Status, "APPROVED", StringComparison.OrdinalIgnoreCase));
            var publishedFlowCount = flows.Count(item =>
                string.Equals(item.Status, "PUBLISHED", StringComparison.OrdinalIgnoreCase));

            var integration = existing ??
                new Integration(
                    condominiumId,
                    IntegrationKind.WhatsApp,
                    info.Id,
                    info.VerifiedName);

            if (existing is null)
            {
                db.Integrations.Add(integration);
            }

            var verifiedAt = clock.UtcNow;
            integration.MarkConnected(info.VerifiedName, verifiedAt);

            db.AuditEvents.Add(
                new AuditEvent(
                    "integration.whatsapp.verified",
                    nameof(Integration),
                    integration.Id.ToString(),
                    "success",
                    verifiedAt,
                    user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue("sub"),
                    httpContext.Items["X-Correlation-ID"]?.ToString()));

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(
                new WhatsAppIntegrationVerifyResponse(
                    integration.Status.ToString(),
                    info.Id,
                    info.VerifiedName,
                    info.DisplayPhoneNumber,
                    info.QualityRating,
                    approvedTemplateCount,
                    publishedFlowCount,
                    verifiedAt));
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            if (existing is not null)
            {
                existing.MarkDegraded("meta_timeout", clock.UtcNow);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            return Results.Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Não foi possível verificar o WhatsApp",
                detail: "A Meta não respondeu dentro do tempo esperado.");
        }
        catch (HttpRequestException exception)
        {
            if (existing is not null)
            {
                existing.MarkDegraded(
                    exception.StatusCode is null
                        ? "meta_transport_failed"
                        : $"meta_http_{(int)exception.StatusCode.Value}",
                    clock.UtcNow);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Não foi possível verificar o WhatsApp",
                detail: "A API oficial da Meta recusou ou não concluiu a solicitação.");
        }
    }

    private static Task<bool> IsConnectedAsync(
        SentraDbContext db,
        Guid condominiumId,
        CancellationToken cancellationToken)
        => db.Integrations.AsNoTracking().AnyAsync(
            item =>
                item.CondominiumId == condominiumId &&
                item.Kind == IntegrationKind.WhatsApp &&
                item.Status == IntegrationStatus.Connected,
            cancellationToken);

    private static bool TryGetCondominiumId(
        ClaimsPrincipal user,
        out Guid condominiumId)
        => Guid.TryParse(
            user.FindFirst("condominium_id")?.Value,
            out condominiumId);
}

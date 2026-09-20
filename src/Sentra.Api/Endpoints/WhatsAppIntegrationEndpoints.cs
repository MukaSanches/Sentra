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
            "/api/v1/integrations/whatsapp/status",
            GetStatusAsync)
            .RequireAuthorization(PermissionCatalog.IntegrationsManage);

        endpoints.MapPost(
            "/api/v1/integrations/whatsapp/verify",
            VerifyAsync)
            .RequireAuthorization(PermissionCatalog.IntegrationsManage);

        return endpoints;
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

        var now = clock.UtcNow;
        var existing = await db.Integrations.SingleOrDefaultAsync(
            item =>
                item.CondominiumId == condominiumId &&
                item.Kind == IntegrationKind.WhatsApp,
            cancellationToken);

        try
        {
            var info = await client.GetPhoneInfoAsync(cancellationToken);
            await client.SubscribeWabaAsync(cancellationToken);

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
                            "O condomínio já está vinculado a outro Phone Number ID. A troca deve ser feita por uma operação administrativa explícita."
                    });
            }

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

            integration.MarkConnected(
                info.VerifiedName,
                now);

            db.AuditEvents.Add(
                new AuditEvent(
                    "integration.whatsapp.verified",
                    nameof(Integration),
                    integration.Id.ToString(),
                    "success",
                    now,
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
                    now));
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            if (existing is not null)
            {
                existing.MarkDegraded("meta_timeout", now);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            return Results.Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Não foi possível verificar o WhatsApp",
                detail: "A Meta não respondeu dentro do tempo esperado.");
        }
        catch (HttpRequestException)
        {
            if (existing is not null)
            {
                existing.MarkDegraded("meta_request_failed", now);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Não foi possível verificar o WhatsApp",
                detail: "A API oficial da Meta recusou ou não concluiu a solicitação.");
        }
    }

    private static bool TryGetCondominiumId(
        ClaimsPrincipal user,
        out Guid condominiumId)
        => Guid.TryParse(
            user.FindFirst("condominium_id")?.Value,
            out condominiumId);
}

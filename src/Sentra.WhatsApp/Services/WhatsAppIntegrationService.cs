using Microsoft.EntityFrameworkCore;
using Sentra.Application.Abstractions;
using Sentra.Contracts.WhatsApp;
using Sentra.Domain.Auditing;
using Sentra.Domain.WhatsApp;
using Sentra.Infrastructure.Persistence;
using Sentra.WhatsApp.Configuration;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp.Services;

public sealed class WhatsAppIntegrationService(
    SentraDbContext dbContext,
    IMetaWhatsAppClient meta,
    WhatsAppOptions options,
    IClock clock) : IWhatsAppIntegrationService
{
    public async Task<WhatsAppConfigurationStatusResponse> GetStatusAsync(
        Guid condominiumId,
        CancellationToken cancellationToken)
    {
        var integration = await dbContext.WhatsAppIntegrations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CondominiumId == condominiumId, cancellationToken);

        var missing = options.Missing();
        if (missing.Count > 0)
            return Response(integration, false, false, false, false, 0, 0, missing, "AGUARDANDO CONFIGURAÇÃO");

        try
        {
            var phones = await meta.GetPhoneNumbersAsync(cancellationToken);
            var phone = phones.SingleOrDefault(x => x.Id == options.PhoneNumberId);
            var subscriptions = await meta.GetSubscriptionsAsync(cancellationToken);
            var subscribed = subscriptions.Any(x => x.AppId == options.AppId);
            var templates = await meta.GetTemplatesAsync(cancellationToken);
            var flows = await meta.GetFlowsAsync(cancellationToken);

            return Response(
                integration,
                true,
                true,
                phone is not null,
                subscribed,
                templates.Count(x => string.Equals(x.Status, "APPROVED", StringComparison.OrdinalIgnoreCase)),
                flows.Count,
                [],
                phone is null ? "PHONE_NUMBER_ID NÃO PERTENCE À WABA" : subscribed ? "PRONTO PARA ATIVAÇÃO" : "APP NÃO INSCRITO NA WABA",
                phone);
        }
        catch (MetaWhatsAppException exception)
        {
            return Response(integration, true, false, false, false, 0, 0, [], $"META HTTP {exception.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return Response(integration, true, false, false, false, 0, 0, [], "META INDISPONÍVEL");
        }
    }

    public async Task<WhatsAppConfigurationStatusResponse> ConfigureAsync(
        Guid condominiumId,
        string actor,
        CancellationToken cancellationToken)
    {
        if (options.Missing().Count > 0)
            return await GetStatusAsync(condominiumId, cancellationToken);

        if (!await dbContext.Condominiums.AnyAsync(x => x.Id == condominiumId, cancellationToken))
            throw new ArgumentException("Condomínio não encontrado.", nameof(condominiumId));

        var phones = await meta.GetPhoneNumbersAsync(cancellationToken);
        var phone = phones.SingleOrDefault(x => x.Id == options.PhoneNumberId);
        if (phone is null)
            return Response(null, true, true, false, false, 0, 0, [], "PHONE_NUMBER_ID NÃO PERTENCE À WABA");

        var subscriptions = await meta.GetSubscriptionsAsync(cancellationToken);
        var subscribed = subscriptions.Any(x => x.AppId == options.AppId);
        if (!subscribed)
        {
            subscribed = await meta.SubscribeAppAsync(cancellationToken);
            if (subscribed)
            {
                subscriptions = await meta.GetSubscriptionsAsync(cancellationToken);
                subscribed = subscriptions.Any(x => x.AppId == options.AppId);
            }
        }

        var templates = await meta.GetTemplatesAsync(cancellationToken);
        var flows = await meta.GetFlowsAsync(cancellationToken);
        var now = clock.UtcNow;

        var integration = await dbContext.WhatsAppIntegrations
            .SingleOrDefaultAsync(x => x.CondominiumId == condominiumId, cancellationToken);

        if (integration is null)
        {
            integration = new WhatsAppIntegration(
                condominiumId,
                options.WabaId,
                options.PhoneNumberId,
                actor,
                now);
            dbContext.WhatsAppIntegrations.Add(integration);
        }
        else if (integration.WabaId != options.WabaId || integration.PhoneNumberId != options.PhoneNumberId)
        {
            throw new InvalidOperationException(
                "A integração existente usa outra WABA/Phone Number ID. Desative-a explicitamente antes de trocar a conta.");
        }

        integration.RecordValidation(
            phone.DisplayPhoneNumber,
            phone.VerifiedName,
            phone.QualityRating,
            subscribed,
            actor,
            now);

        dbContext.AuditEvents.Add(new AuditEvent(
            "whatsapp.integration.validated",
            nameof(WhatsAppIntegration),
            integration.Id.ToString(),
            subscribed ? "success" : "partial",
            now,
            actor));

        await dbContext.SaveChangesAsync(cancellationToken);

        return Response(
            integration,
            true,
            true,
            true,
            subscribed,
            templates.Count(x => string.Equals(x.Status, "APPROVED", StringComparison.OrdinalIgnoreCase)),
            flows.Count,
            [],
            subscribed ? "CONFIGURADO" : "WEBHOOK AINDA NÃO INSCRITO",
            phone);
    }

    private WhatsAppConfigurationStatusResponse Response(
        WhatsAppIntegration? integration,
        bool configurationPresent,
        bool apiReachable,
        bool phoneValidated,
        bool appSubscribed,
        int approvedTemplates,
        int flows,
        IReadOnlyList<string> missing,
        string state,
        MetaPhoneNumber? phone = null)
        => new(
            configurationPresent,
            apiReachable,
            apiReachable,
            phoneValidated,
            appSubscribed,
            phone?.DisplayPhoneNumber ?? integration?.DisplayPhoneNumber,
            phone?.VerifiedName ?? integration?.VerifiedName,
            phone?.QualityRating ?? integration?.QualityRating,
            approvedTemplates,
            flows,
            integration?.LastValidatedAt,
            integration?.LastWebhookAt,
            BuildWebhookUrl(),
            missing,
            state);

    private string BuildWebhookUrl()
        => string.IsNullOrWhiteSpace(options.PublicBaseUrl)
            ? "/api/webhooks/whatsapp"
            : $"{options.PublicBaseUrl.TrimEnd('/')}/api/webhooks/whatsapp";
}

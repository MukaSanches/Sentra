using Sentra.Contracts.WhatsApp;

namespace Sentra.WhatsApp.Services;

public interface IWhatsAppIntegrationService
{
    Task<WhatsAppConfigurationStatusResponse> GetStatusAsync(
        Guid condominiumId,
        CancellationToken cancellationToken);

    Task<WhatsAppConfigurationStatusResponse> ConfigureAsync(
        Guid condominiumId,
        string actor,
        CancellationToken cancellationToken);
}

namespace Sentra.WhatsApp.Services;

public interface IWhatsAppWebhookProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}

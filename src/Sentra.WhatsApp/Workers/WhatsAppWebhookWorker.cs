using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentra.WhatsApp.Services;

namespace Sentra.WhatsApp.Workers;

public sealed class WhatsAppWebhookWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<WhatsAppWebhookWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IWhatsAppWebhookProcessor>();
                var processed = await processor.ProcessNextAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "WhatsApp webhook worker failed with {ExceptionType}. Payload content is intentionally omitted.",
                    exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

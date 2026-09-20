using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentra.Application.Integrations.WhatsApp;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp;

public static class DependencyInjection
{
    public static IServiceCollection AddSentraWhatsApp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IWhatsAppWebhookVerifier, MetaWebhookVerifier>();
        services.AddSingleton<IWhatsAppWebhookParser, MetaWebhookParser>();

        services.AddHttpClient<IWhatsAppClient, MetaWhatsAppClient>(client =>
        {
            client.BaseAddress = new Uri("https://graph.facebook.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static bool IsSentraWhatsAppConfigured(
        this IConfiguration configuration)
        => MetaWhatsAppConfiguration.IsConfigured(configuration);
}

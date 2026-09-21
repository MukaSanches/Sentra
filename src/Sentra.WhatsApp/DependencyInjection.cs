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

    public static IReadOnlyList<string> GetSentraWhatsAppConfigurationIssues(
        this IConfiguration configuration)
    {
        var required = new[]
        {
            "META_GRAPH_VERSION",
            "META_WABA_ID",
            "META_PHONE_NUMBER_ID",
            "META_ACCESS_TOKEN",
            "META_VERIFY_TOKEN",
            "META_APP_SECRET"
        };

        var issues = required
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .Select(key => $"{key}: ausente")
            .ToList();

        if (issues.Count == 0)
        {
            try
            {
                _ = MetaWhatsAppConfiguration.From(configuration);
            }
            catch (InvalidOperationException exception)
            {
                issues.Add(exception.Message);
            }
        }

        return issues;
    }
}

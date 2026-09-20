using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentra.WhatsApp.Configuration;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp;

public static class DependencyInjection
{
    public static IServiceCollection AddSentraWhatsApp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new WhatsAppOptions
        {
            GraphApiVersion = configuration["META_GRAPH_API_VERSION"] ?? string.Empty,
            AppId = configuration["META_APP_ID"] ?? string.Empty,
            WabaId = configuration["META_WABA_ID"] ?? string.Empty,
            PhoneNumberId = configuration["META_PHONE_NUMBER_ID"] ?? string.Empty,
            AccessToken = configuration["META_ACCESS_TOKEN"] ?? string.Empty,
            VerifyToken = configuration["META_VERIFY_TOKEN"] ?? string.Empty,
            AppSecret = configuration["META_APP_SECRET"] ?? string.Empty,
            PublicBaseUrl = configuration["PUBLIC_BASE_URL"] ?? string.Empty
        };

        services.AddSingleton(options);
        services.AddHttpClient("MetaWhatsApp", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IMetaWhatsAppClient, MetaWhatsAppClient>();
        return services;
    }
}

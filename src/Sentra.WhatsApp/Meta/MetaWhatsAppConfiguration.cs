using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Sentra.WhatsApp.Meta;

internal sealed record MetaWhatsAppConfiguration(
    string GraphVersion,
    string PhoneNumberId,
    string WabaId,
    string AccessToken,
    string VerifyToken,
    string AppSecret)
{
    private static readonly Regex GraphVersionPattern =
        new("^v[0-9]+\\.[0-9]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex NumericIdPattern =
        new("^[0-9]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static MetaWhatsAppConfiguration From(IConfiguration configuration)
    {
        var graphVersion = Require(configuration, "META_GRAPH_VERSION");
        var phoneNumberId = Require(configuration, "META_PHONE_NUMBER_ID");
        var wabaId = Require(configuration, "META_WABA_ID");
        var accessToken = Require(configuration, "META_ACCESS_TOKEN");
        var verifyToken = Require(configuration, "META_VERIFY_TOKEN");
        var appSecret = Require(configuration, "META_APP_SECRET");

        if (!GraphVersionPattern.IsMatch(graphVersion))
        {
            throw new InvalidOperationException(
                "META_GRAPH_VERSION deve estar no formato vN.N e deve ser uma versão suportada pela Meta.");
        }

        if (!NumericIdPattern.IsMatch(phoneNumberId) ||
            !NumericIdPattern.IsMatch(wabaId))
        {
            throw new InvalidOperationException(
                "META_PHONE_NUMBER_ID e META_WABA_ID devem conter somente dígitos.");
        }

        if (verifyToken.Length < 16)
        {
            throw new InvalidOperationException(
                "META_VERIFY_TOKEN deve possuir pelo menos 16 caracteres.");
        }

        if (appSecret.Length < 16)
        {
            throw new InvalidOperationException(
                "META_APP_SECRET parece inválido.");
        }

        return new(
            graphVersion,
            phoneNumberId,
            wabaId,
            accessToken,
            verifyToken,
            appSecret);
    }

    public static bool IsConfigured(IConfiguration configuration)
    {
        try
        {
            _ = From(configuration);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} não está configurado.");
        }

        return value.Trim();
    }
}

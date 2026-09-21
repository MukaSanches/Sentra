using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Sentra.Application.Integrations.WhatsApp;

namespace Sentra.WhatsApp.Meta;

public sealed class MetaWebhookVerifier(IConfiguration configuration)
    : IWhatsAppWebhookVerifier
{
    public bool VerifyChallenge(string? mode, string? suppliedVerifyToken)
    {
        if (!string.Equals(mode, "subscribe", StringComparison.Ordinal))
        {
            return false;
        }

        MetaWhatsAppConfiguration settings;

        try
        {
            settings = MetaWhatsAppConfiguration.From(configuration);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return FixedTimeEquals(settings.VerifyToken, suppliedVerifyToken);
    }

    public bool VerifySignature(
        ReadOnlySpan<byte> payload,
        string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) ||
            !signatureHeader.StartsWith("sha256=", StringComparison.Ordinal))
        {
            return false;
        }

        MetaWhatsAppConfiguration settings;

        try
        {
            settings = MetaWhatsAppConfiguration.From(configuration);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        byte[] supplied;

        try
        {
            supplied = Convert.FromHexString(signatureHeader["sha256=".Length..]);
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(settings.AppSecret));
        var expected = hmac.ComputeHash(payload.ToArray());

        return supplied.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(supplied, expected);
    }

    private static bool FixedTimeEquals(string expected, string? supplied)
    {
        if (supplied is null)
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);

        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}

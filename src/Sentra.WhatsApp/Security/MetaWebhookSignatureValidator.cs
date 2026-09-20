using System.Security.Cryptography;
using System.Text;

namespace Sentra.WhatsApp.Security;

public static class MetaWebhookSignatureValidator
{
    private const string Prefix = "sha256=";

    public static bool IsValid(ReadOnlySpan<byte> payload, string? signatureHeader, string appSecret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader)
            || string.IsNullOrWhiteSpace(appSecret)
            || !signatureHeader.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        byte[] supplied;
        try
        {
            var suppliedHex = signatureHeader[Prefix.Length..];
            if (suppliedHex.Length != 64) return false;
            supplied = Convert.FromHexString(suppliedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var expected = hmac.ComputeHash(payload.ToArray());
        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }

    public static string ComputeEventHash(ReadOnlySpan<byte> payload)
        => Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
}

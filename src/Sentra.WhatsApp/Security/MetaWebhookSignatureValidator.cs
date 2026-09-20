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

        var suppliedHex = signatureHeader[Prefix.Length..];
        if (suppliedHex.Length != 64) return false;

        Span<byte> supplied = stackalloc byte[32];
        if (!Convert.TryFromHexString(suppliedHex, supplied, out var bytesWritten) || bytesWritten != 32)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        Span<byte> expected = stackalloc byte[32];
        if (!hmac.TryComputeHash(payload, expected, out var expectedWritten) || expectedWritten != 32)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }

    public static string ComputeEventHash(ReadOnlySpan<byte> payload)
        => Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
}

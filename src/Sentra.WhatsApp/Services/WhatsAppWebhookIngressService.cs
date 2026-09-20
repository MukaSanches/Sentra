using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Abstractions;
using Sentra.Domain.WhatsApp;
using Sentra.Infrastructure.Persistence;
using Sentra.WhatsApp.Configuration;
using Sentra.WhatsApp.Security;

namespace Sentra.WhatsApp.Services;

public sealed class WhatsAppWebhookIngressService(
    SentraDbContext dbContext,
    WhatsAppOptions options,
    IClock clock) : IWhatsAppWebhookIngressService
{
    public bool TryVerifyChallenge(
        string? mode,
        string? verifyToken,
        string? challenge,
        out string response)
    {
        response = string.Empty;
        if (!string.Equals(mode, "subscribe", StringComparison.Ordinal)
            || string.IsNullOrEmpty(challenge)
            || string.IsNullOrWhiteSpace(options.VerifyToken)
            || !FixedEquals(verifyToken, options.VerifyToken))
        {
            return false;
        }

        response = challenge;
        return true;
    }

    public async Task<WebhookAcceptance> AcceptAsync(
        ReadOnlyMemory<byte> payload,
        string? signature,
        CancellationToken cancellationToken)
    {
        if (payload.IsEmpty)
            return new(false, false, "EMPTY_PAYLOAD");

        if (!MetaWebhookSignatureValidator.IsValid(payload.Span, signature, options.AppSecret))
            return new(false, false, "INVALID_SIGNATURE");

        string json;
        try
        {
            json = Encoding.UTF8.GetString(payload.Span);
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("object", out var objectName)
                || !string.Equals(objectName.GetString(), "whatsapp_business_account", StringComparison.Ordinal))
            {
                return new(false, false, "INVALID_OBJECT");
            }
        }
        catch (JsonException)
        {
            return new(false, false, "INVALID_JSON");
        }

        var hash = MetaWebhookSignatureValidator.ComputeEventHash(payload.Span);
        if (await dbContext.WhatsAppWebhookEvents.AsNoTracking()
            .AnyAsync(x => x.EventHash == hash, cancellationToken))
        {
            return new(true, true, null);
        }

        dbContext.WhatsAppWebhookEvents.Add(new WhatsAppWebhookEvent(hash, json, clock.UtcNow));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(true, false, null);
        }
        catch (DbUpdateException)
        {
            if (await dbContext.WhatsAppWebhookEvents.AsNoTracking()
                .AnyAsync(x => x.EventHash == hash, cancellationToken))
            {
                return new(true, true, null);
            }

            throw;
        }
    }

    private static bool FixedEquals(string? left, string right)
    {
        if (left is null) return false;
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

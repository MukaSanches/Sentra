namespace Sentra.WhatsApp.Services;

public sealed record WebhookAcceptance(bool Accepted, bool Duplicate, string? Error);

public interface IWhatsAppWebhookIngressService
{
    bool TryVerifyChallenge(string? mode, string? verifyToken, string? challenge, out string response);

    Task<WebhookAcceptance> AcceptAsync(
        ReadOnlyMemory<byte> payload,
        string? signature,
        CancellationToken cancellationToken);
}

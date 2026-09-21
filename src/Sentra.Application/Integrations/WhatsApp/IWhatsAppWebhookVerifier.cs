namespace Sentra.Application.Integrations.WhatsApp;

public interface IWhatsAppWebhookVerifier
{
    bool VerifyChallenge(string? mode, string? suppliedVerifyToken);
    bool VerifySignature(ReadOnlySpan<byte> payload, string? signatureHeader);
}

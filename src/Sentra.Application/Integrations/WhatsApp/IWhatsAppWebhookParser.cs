namespace Sentra.Application.Integrations.WhatsApp;

public enum WhatsAppIncomingKind
{
    Unknown = 0,
    Text = 1,
    Image = 2,
    Audio = 3,
    Video = 4,
    Document = 5,
    Location = 6,
    Contacts = 7,
    Interactive = 8,
    Reaction = 9
}

public sealed record WhatsAppIncomingMessage(
    string FromWaId,
    string MessageId,
    DateTimeOffset Timestamp,
    WhatsAppIncomingKind Kind,
    string? Text,
    string? MediaId,
    string? MimeType,
    string? FileName,
    string? Sha256,
    string? StructuredDataJson);

public sealed record WhatsAppStatusUpdate(
    string MessageId,
    string Status,
    DateTimeOffset Timestamp,
    string? RecipientWaId,
    string? ErrorCode,
    string? ErrorTitle);

public sealed record WhatsAppWebhookChange(
    string PhoneNumberId,
    string? ContactName,
    string? ContactWaId,
    IReadOnlyList<WhatsAppIncomingMessage> Messages,
    IReadOnlyList<WhatsAppStatusUpdate> Statuses);

public sealed record WhatsAppWebhookEnvelope(
    IReadOnlyList<WhatsAppWebhookChange> Changes);

public interface IWhatsAppWebhookParser
{
    WhatsAppWebhookEnvelope Parse(string json);
}

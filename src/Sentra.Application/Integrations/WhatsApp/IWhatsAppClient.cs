namespace Sentra.Application.Integrations.WhatsApp;

public sealed record WhatsAppPhoneInfo(
    string Id,
    string VerifiedName,
    string DisplayPhoneNumber,
    string QualityRating);

public sealed record WhatsAppSendResult(string MessageId);

public sealed record WhatsAppMediaInfo(
    string Id,
    Uri Url,
    string? MimeType,
    string? Sha256,
    long? FileSize);

public interface IWhatsAppClient
{
    Task<WhatsAppPhoneInfo> GetPhoneInfoAsync(CancellationToken cancellationToken);
    Task SubscribeWabaAsync(CancellationToken cancellationToken);
    Task<WhatsAppSendResult> SendTextAsync(
        string recipientE164,
        string text,
        CancellationToken cancellationToken);
    Task<WhatsAppMediaInfo> GetMediaInfoAsync(
        string mediaId,
        CancellationToken cancellationToken);
    Task<HttpResponseMessage> DownloadMediaAsync(
        Uri mediaUrl,
        CancellationToken cancellationToken);
}

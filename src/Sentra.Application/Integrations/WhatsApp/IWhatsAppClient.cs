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

public sealed record WhatsAppTemplateInfo(
    string Id,
    string Name,
    string Language,
    string Status,
    string Category);

public sealed record WhatsAppFlowInfo(
    string Id,
    string Name,
    string Status);

public sealed record WhatsAppInteractiveButton(
    string Id,
    string Title);

public sealed record WhatsAppListRow(
    string Id,
    string Title,
    string? Description);

public sealed record WhatsAppListSection(
    string? Title,
    IReadOnlyList<WhatsAppListRow> Rows);

public sealed record WhatsAppMediaUploadResult(string MediaId);

public enum WhatsAppMediaKind
{
    Image = 1,
    Audio = 2,
    Video = 3,
    Document = 4
}

public interface IWhatsAppClient
{
    Task<WhatsAppPhoneInfo> GetPhoneInfoAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WhatsAppPhoneInfo>> GetWabaPhoneNumbersAsync(
        CancellationToken cancellationToken);

    Task SubscribeWabaAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WhatsAppTemplateInfo>> GetTemplatesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WhatsAppFlowInfo>> GetFlowsAsync(
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendTextAsync(
        string recipientE164,
        string text,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendTemplateAsync(
        string recipientE164,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendReplyButtonsAsync(
        string recipientE164,
        string body,
        IReadOnlyList<WhatsAppInteractiveButton> buttons,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendListAsync(
        string recipientE164,
        string body,
        string buttonText,
        IReadOnlyList<WhatsAppListSection> sections,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendFlowAsync(
        string recipientE164,
        string flowId,
        string flowToken,
        string callToAction,
        string body,
        string? screen,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken);

    Task MarkMessageReadAsync(
        string messageId,
        CancellationToken cancellationToken);

    Task MarkMessageReadWithTypingAsync(
        string messageId,
        CancellationToken cancellationToken);

    Task<WhatsAppMediaInfo> GetMediaInfoAsync(
        string mediaId,
        CancellationToken cancellationToken);

    Task DownloadMediaAsync(
        string mediaId,
        Stream destination,
        CancellationToken cancellationToken);

    Task<WhatsAppMediaUploadResult> UploadMediaAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendMediaAsync(
        string recipientE164,
        WhatsAppMediaKind kind,
        string mediaId,
        string? caption,
        string? fileName,
        CancellationToken cancellationToken);
}

using System.Text.Json;
using Sentra.Contracts.WhatsApp;

namespace Sentra.WhatsApp.Meta;

public interface IMetaWhatsAppClient
{
    Task<IReadOnlyList<MetaPhoneNumber>> GetPhoneNumbersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaSubscription>> GetSubscriptionsAsync(CancellationToken cancellationToken);
    Task<bool> SubscribeAppAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaTemplate>> GetTemplatesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaFlow>> GetFlowsAsync(CancellationToken cancellationToken);
    Task<MetaSendResult> SendTextAsync(string to, string text, string? replyToMessageId, CancellationToken cancellationToken);
    Task<MetaSendResult> SendButtonsAsync(string to, string body, IReadOnlyList<WhatsAppReplyButton> buttons, CancellationToken cancellationToken);
    Task<MetaSendResult> SendListAsync(string to, string body, string buttonText, IReadOnlyList<WhatsAppListSection> sections, CancellationToken cancellationToken);
    Task<MetaSendResult> SendTemplateAsync(string to, string templateName, string languageCode, IReadOnlyList<string>? bodyParameters, CancellationToken cancellationToken);
    Task<MetaSendResult> SendMediaAsync(string to, string mediaId, string mediaType, string? caption, string? fileName, CancellationToken cancellationToken);
    Task<MetaSendResult> SendFlowAsync(SendWhatsAppFlowRequest request, CancellationToken cancellationToken);
    Task<bool> MarkReadAsync(string messageId, CancellationToken cancellationToken);
    Task<MetaMediaMetadata> GetMediaMetadataAsync(string mediaId, CancellationToken cancellationToken);
    Task<byte[]> DownloadMediaAsync(string absoluteUrl, CancellationToken cancellationToken);
    Task<string> UploadMediaAsync(Stream stream, string fileName, string mimeType, CancellationToken cancellationToken);
}

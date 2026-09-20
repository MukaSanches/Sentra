using Sentra.Domain.Common;

namespace Sentra.Domain.WhatsApp;

public sealed class WhatsAppAttachment : EntityBase
{
    private WhatsAppAttachment() { }

    public WhatsAppAttachment(
        Guid condominiumId,
        Guid messageId,
        string mediaId,
        string mimeType,
        DateTimeOffset createdAt,
        string? fileName = null,
        string? sha256 = null,
        long? fileSize = null)
        : base(createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        MessageId = Guard.RequiredId(messageId, nameof(messageId));
        MediaId = Guard.Required(mediaId, nameof(mediaId), 128);
        MimeType = Guard.Required(mimeType, nameof(mimeType), 160);
        FileName = Guard.Optional(fileName, nameof(fileName), 255);
        Sha256 = Guard.Optional(sha256, nameof(sha256), 128);
        FileSize = fileSize;
    }

    public Guid CondominiumId { get; private set; }
    public Guid MessageId { get; private set; }
    public string MediaId { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public string? FileName { get; private set; }
    public string? Sha256 { get; private set; }
    public long? FileSize { get; private set; }
    public byte[]? Content { get; private set; }
    public DateTimeOffset? DownloadedAt { get; private set; }

    public void Store(byte[] content, DateTimeOffset downloadedAt)
    {
        ArgumentNullException.ThrowIfNull(content);
        Content = content;
        DownloadedAt = downloadedAt;
        MarkUpdated(downloadedAt);
    }
}

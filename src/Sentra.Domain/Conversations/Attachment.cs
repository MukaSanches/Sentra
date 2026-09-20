using Sentra.Domain.Common;

namespace Sentra.Domain.Conversations;

public enum AttachmentStorageStatus
{
    Pending = 0,
    AwaitingConfiguration = 1,
    Stored = 2,
    Failed = 3
}

public sealed class Attachment : EntityBase
{
    private Attachment()
    {
    }

    public Attachment(
        Guid messageId,
        string externalMediaId,
        string? mimeType,
        string? fileName,
        string? sha256)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Mensagem inválida.", nameof(messageId));
        }

        MessageId = messageId;
        ExternalMediaId = Required(externalMediaId, nameof(externalMediaId), 160);
        MimeType = Optional(mimeType, 160);
        FileName = Optional(fileName, 255);
        Sha256 = Optional(sha256, 128);
        StorageStatus = AttachmentStorageStatus.Pending;
    }

    public Guid MessageId { get; private set; }
    public string ExternalMediaId { get; private set; } = string.Empty;
    public string? MimeType { get; private set; }
    public string? FileName { get; private set; }
    public string? Sha256 { get; private set; }
    public AttachmentStorageStatus StorageStatus { get; private set; }
    public string? StorageKey { get; private set; }

    public void MarkAwaitingConfiguration(DateTimeOffset timestamp)
    {
        StorageStatus = AttachmentStorageStatus.AwaitingConfiguration;
        MarkUpdated(timestamp);
    }

    private static string Required(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O valor é obrigatório.", name);
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : throw new ArgumentOutOfRangeException(name);
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(value));
    }
}

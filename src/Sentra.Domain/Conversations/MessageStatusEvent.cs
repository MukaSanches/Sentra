using Sentra.Domain.Common;

namespace Sentra.Domain.Conversations;

public sealed class MessageStatusEvent : EntityBase
{
    private MessageStatusEvent()
    {
    }

    public MessageStatusEvent(
        string externalMessageId,
        MessageDeliveryStatus status,
        DateTimeOffset occurredAt,
        string? recipientWaId,
        string? errorCode)
        : base(occurredAt)
    {
        ExternalMessageId = Required(
            externalMessageId,
            nameof(externalMessageId),
            256);
        Status = status;
        OccurredAt = occurredAt;
        RecipientWaId = Optional(recipientWaId, 32);
        ErrorCode = Optional(errorCode, 96);
    }

    public string ExternalMessageId { get; private set; } = string.Empty;
    public MessageDeliveryStatus Status { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? RecipientWaId { get; private set; }
    public string? ErrorCode { get; private set; }

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

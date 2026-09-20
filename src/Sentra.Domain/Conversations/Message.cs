using Sentra.Domain.Common;

namespace Sentra.Domain.Conversations;

public enum MessageDirection
{
    Inbound = 0,
    Outbound = 1
}

public enum MessageContentKind
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

public enum MessageDeliveryStatus
{
    Pending = 0,
    Received = 1,
    Accepted = 2,
    Sent = 3,
    Delivered = 4,
    Read = 5,
    Failed = 6
}

public sealed class Message : EntityBase
{
    private Message()
    {
    }

    private Message(
        Guid conversationId,
        MessageDirection direction,
        MessageContentKind contentKind,
        string? text,
        DateTimeOffset occurredAt,
        string? externalMessageId,
        Guid? clientRequestId,
        MessageDeliveryStatus deliveryStatus)
        : base(occurredAt)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversa inválida.", nameof(conversationId));
        }

        ConversationId = conversationId;
        Direction = direction;
        ContentKind = contentKind;
        Text = Optional(text, 4096);
        OccurredAt = occurredAt;
        ExternalMessageId = Optional(externalMessageId, 256);
        ClientRequestId = clientRequestId;
        DeliveryStatus = deliveryStatus;
        DeliveryStatusAt = occurredAt;
    }

    public Guid ConversationId { get; private set; }
    public MessageDirection Direction { get; private set; }
    public MessageContentKind ContentKind { get; private set; }
    public string? Text { get; private set; }
    public string? ExternalMessageId { get; private set; }
    public Guid? ClientRequestId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public MessageDeliveryStatus DeliveryStatus { get; private set; }
    public DateTimeOffset DeliveryStatusAt { get; private set; }
    public string? LastErrorCode { get; private set; }

    public static Message CreateInbound(
        Guid conversationId,
        string externalMessageId,
        MessageContentKind kind,
        string? text,
        DateTimeOffset occurredAt)
        => new(
            conversationId,
            MessageDirection.Inbound,
            kind,
            text,
            occurredAt,
            externalMessageId,
            null,
            MessageDeliveryStatus.Received);

    public static Message CreateOutboundPending(
        Guid conversationId,
        Guid clientRequestId,
        string text,
        DateTimeOffset occurredAt)
    {
        if (clientRequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "Idempotency key inválida.",
                nameof(clientRequestId));
        }

        return new(
            conversationId,
            MessageDirection.Outbound,
            MessageContentKind.Text,
            text,
            occurredAt,
            null,
            clientRequestId,
            MessageDeliveryStatus.Pending);
    }

    public void MarkAccepted(
        string externalMessageId,
        DateTimeOffset timestamp)
    {
        ExternalMessageId = Required(
            externalMessageId,
            nameof(externalMessageId),
            256);
        DeliveryStatus = MessageDeliveryStatus.Accepted;
        DeliveryStatusAt = timestamp;
        LastErrorCode = null;
        MarkUpdated(timestamp);
    }

    public bool ApplyDeliveryStatus(
        MessageDeliveryStatus status,
        DateTimeOffset statusTimestamp,
        string? errorCode = null)
    {
        if (statusTimestamp < DeliveryStatusAt)
        {
            return false;
        }

        DeliveryStatus = status;
        DeliveryStatusAt = statusTimestamp;
        LastErrorCode = Optional(errorCode, 96);
        MarkUpdated(statusTimestamp > UpdatedAt ? statusTimestamp : UpdatedAt);
        return true;
    }

    public void MarkSendFailure(
        string errorCode,
        DateTimeOffset timestamp)
    {
        DeliveryStatus = MessageDeliveryStatus.Failed;
        DeliveryStatusAt = timestamp;
        LastErrorCode = Required(errorCode, nameof(errorCode), 96);
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

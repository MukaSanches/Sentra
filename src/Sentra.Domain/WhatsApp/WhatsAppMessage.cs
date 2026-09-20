using Sentra.Domain.Common;

namespace Sentra.Domain.WhatsApp;

public sealed class WhatsAppMessage : EntityBase
{
    private WhatsAppMessage() { }

    public WhatsAppMessage(
        Guid condominiumId,
        Guid conversationId,
        WhatsAppMessageDirection direction,
        WhatsAppMessageType type,
        DateTimeOffset messageTimestamp,
        string? externalMessageId = null,
        string? text = null,
        string? replyToExternalMessageId = null,
        string? rawPayloadJson = null)
        : base(messageTimestamp)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        ConversationId = Guard.RequiredId(conversationId, nameof(conversationId));
        Direction = direction;
        Type = type;
        ExternalMessageId = Guard.Optional(externalMessageId, nameof(externalMessageId), 256);
        Text = Guard.Optional(text, nameof(text), 8192);
        ReplyToExternalMessageId = Guard.Optional(replyToExternalMessageId, nameof(replyToExternalMessageId), 256);
        RawPayloadJson = rawPayloadJson;
        MessageTimestamp = messageTimestamp;
        DeliveryStatus = direction == WhatsAppMessageDirection.Inbound
            ? WhatsAppDeliveryStatus.Received
            : WhatsAppDeliveryStatus.Pending;
        DeliveryStatusAt = messageTimestamp;
    }

    public Guid CondominiumId { get; private set; }
    public Guid ConversationId { get; private set; }
    public string? ExternalMessageId { get; private set; }
    public WhatsAppMessageDirection Direction { get; private set; }
    public WhatsAppMessageType Type { get; private set; }
    public string? Text { get; private set; }
    public string? ReplyToExternalMessageId { get; private set; }
    public string? RawPayloadJson { get; private set; }
    public DateTimeOffset MessageTimestamp { get; private set; }
    public WhatsAppDeliveryStatus DeliveryStatus { get; private set; }
    public DateTimeOffset DeliveryStatusAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorTitle { get; private set; }

    public void SetExternalMessageId(string externalMessageId)
        => ExternalMessageId = Guard.Required(externalMessageId, nameof(externalMessageId), 256);

    public bool ApplyDeliveryStatus(
        WhatsAppDeliveryStatus status,
        DateTimeOffset statusTimestamp,
        string? errorCode = null,
        string? errorTitle = null)
    {
        if (statusTimestamp < DeliveryStatusAt) return false;

        DeliveryStatus = status;
        DeliveryStatusAt = statusTimestamp;
        ErrorCode = Guard.Optional(errorCode, nameof(errorCode), 80);
        ErrorTitle = Guard.Optional(errorTitle, nameof(errorTitle), 500);
        MarkUpdated(statusTimestamp);
        return true;
    }
}

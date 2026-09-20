namespace Sentra.Domain.WhatsApp;

public enum WhatsAppMessageDirection
{
    Inbound = 1,
    Outbound = 2
}

public enum WhatsAppMessageType
{
    Text = 1,
    Image = 2,
    Audio = 3,
    Document = 4,
    Video = 5,
    Sticker = 6,
    Interactive = 7,
    Button = 8,
    Reaction = 9,
    Location = 10,
    Contacts = 11,
    Template = 12,
    Flow = 13,
    Unknown = 99
}

public enum WhatsAppDeliveryStatus
{
    Received = 1,
    Pending = 2,
    Sent = 3,
    Delivered = 4,
    Read = 5,
    Failed = 6,
    Deleted = 7
}

public enum WebhookProcessingStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4
}

using Sentra.Domain.Common;

namespace Sentra.Domain.WhatsApp;

public sealed class WhatsAppWebhookEvent : EntityBase
{
    private WhatsAppWebhookEvent() { }

    public WhatsAppWebhookEvent(
        string eventHash,
        string payloadJson,
        DateTimeOffset receivedAt)
        : base(receivedAt)
    {
        EventHash = Guard.Required(eventHash, nameof(eventHash), 64);
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson)
            ? throw new ArgumentException("Payload is required.", nameof(payloadJson))
            : payloadJson;
        ReceivedAt = receivedAt;
        ProcessingStatus = WebhookProcessingStatus.Pending;
    }

    public string EventHash { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
    public WebhookProcessingStatus ProcessingStatus { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    public void MarkProcessing(DateTimeOffset timestamp)
    {
        ProcessingStatus = WebhookProcessingStatus.Processing;
        Attempts++;
        MarkUpdated(timestamp);
    }

    public void MarkProcessed(DateTimeOffset timestamp)
    {
        ProcessingStatus = WebhookProcessingStatus.Processed;
        ProcessedAt = timestamp;
        LastError = null;
        MarkUpdated(timestamp);
    }

    public void MarkFailed(string error, DateTimeOffset timestamp)
    {
        ProcessingStatus = WebhookProcessingStatus.Failed;
        LastError = Guard.Optional(error, nameof(error), 2000);
        MarkUpdated(timestamp);
    }

    public void Retry(DateTimeOffset timestamp)
    {
        ProcessingStatus = WebhookProcessingStatus.Pending;
        MarkUpdated(timestamp);
    }
}

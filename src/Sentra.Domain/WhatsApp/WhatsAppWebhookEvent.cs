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
    public DateTimeOffset? NextAttemptAt { get; private set; }

    public bool IsReady(DateTimeOffset now)
        => ProcessingStatus == WebhookProcessingStatus.Pending
            && (!NextAttemptAt.HasValue || NextAttemptAt.Value <= now);

    public void MarkProcessing(DateTimeOffset timestamp)
    {
        ProcessingStatus = WebhookProcessingStatus.Processing;
        Attempts++;
        NextAttemptAt = null;
        MarkUpdated(timestamp);
    }

    public void MarkProcessed(DateTimeOffset timestamp)
    {
        ProcessingStatus = WebhookProcessingStatus.Processed;
        ProcessedAt = timestamp;
        LastError = null;
        NextAttemptAt = null;
        MarkUpdated(timestamp);
    }

    public void ScheduleRetry(string error, DateTimeOffset timestamp)
    {
        LastError = Guard.Optional(error, nameof(error), 2000);
        if (Attempts >= 5)
        {
            ProcessingStatus = WebhookProcessingStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            ProcessingStatus = WebhookProcessingStatus.Pending;
            var delaySeconds = Math.Min(300, 5 * (int)Math.Pow(2, Math.Max(0, Attempts - 1)));
            NextAttemptAt = timestamp.AddSeconds(delaySeconds);
        }
        MarkUpdated(timestamp);
    }

    public void Retry(DateTimeOffset timestamp)
    {
        ProcessingStatus = WebhookProcessingStatus.Pending;
        LastError = null;
        NextAttemptAt = timestamp;
        MarkUpdated(timestamp);
    }
}

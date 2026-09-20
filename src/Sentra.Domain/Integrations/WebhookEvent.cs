using Sentra.Domain.Common;

namespace Sentra.Domain.Integrations;

public enum WebhookEventStatus
{
    Received = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3,
    DeadLetter = 4
}

public sealed class WebhookEvent : EntityBase
{
    private WebhookEvent()
    {
    }

    public WebhookEvent(
        string provider,
        string payloadHash,
        string payloadJson,
        DateTimeOffset receivedAt)
        : base(receivedAt)
    {
        Provider = Required(provider, nameof(provider), 64);
        PayloadHash = Required(payloadHash, nameof(payloadHash), 128);
        PayloadJson = Required(payloadJson, nameof(payloadJson), 1_048_576);
        ReceivedAt = receivedAt;
        Status = WebhookEventStatus.Received;
    }

    public string Provider { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
    public WebhookEventStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public string? LastErrorCode { get; private set; }

    public void MarkProcessing(DateTimeOffset timestamp)
    {
        if (Status is WebhookEventStatus.Processed or WebhookEventStatus.DeadLetter)
        {
            throw new InvalidOperationException("Evento já foi encerrado.");
        }

        Status = WebhookEventStatus.Processing;
        AttemptCount++;
        NextAttemptAt = null;
        MarkUpdated(timestamp);
    }

    public void MarkProcessed(DateTimeOffset timestamp)
    {
        Status = WebhookEventStatus.Processed;
        ProcessedAt = timestamp;
        NextAttemptAt = null;
        LastErrorCode = null;
        MarkUpdated(timestamp);
    }

    public void MarkFailed(
        string errorCode,
        DateTimeOffset timestamp,
        TimeSpan retryDelay,
        int maxAttempts)
    {
        LastErrorCode = Required(errorCode, nameof(errorCode), 96);

        if (AttemptCount >= maxAttempts)
        {
            Status = WebhookEventStatus.DeadLetter;
            NextAttemptAt = null;
        }
        else
        {
            Status = WebhookEventStatus.Failed;
            NextAttemptAt = timestamp.Add(retryDelay);
        }

        MarkUpdated(timestamp);
    }

    private static string Required(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O valor é obrigatório.", parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return normalized;
    }
}

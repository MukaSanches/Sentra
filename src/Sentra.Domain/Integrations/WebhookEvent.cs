using Sentra.Domain.Common;

namespace Sentra.Domain.Integrations;

public enum WebhookProcessingStatus
{
    Received = 0,
    Processed = 1,
    Rejected = 2,
    Failed = 3
}

public sealed class WebhookEvent : EntityBase
{
    private WebhookEvent() { }

    public WebhookEvent(string provider, string externalEventId, string payloadHash)
    {
        Provider = Required(provider, nameof(provider), 64);
        ExternalEventId = Required(externalEventId, nameof(externalEventId), 256);
        PayloadHash = Required(payloadHash, nameof(payloadHash), 128);
        ReceivedAt = DateTimeOffset.UtcNow;
        Status = WebhookProcessingStatus.Received;
    }

    public string Provider { get; private set; } = string.Empty;
    public string ExternalEventId { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public WebhookProcessingStatus Status { get; private set; }

    public void MarkProcessed()
    {
        if (Status == WebhookProcessingStatus.Processed) return;
        Status = WebhookProcessingStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    private static string Required(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("O valor é obrigatório.", name);
        value = value.Trim();
        if (value.Length > maxLength) throw new ArgumentOutOfRangeException(name, $"O valor excede {maxLength} caracteres.");
        return value;
    }
}

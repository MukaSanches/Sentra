using Sentra.Domain.Common;

namespace Sentra.Domain.Auditing;

public sealed class AuditEvent : EntityBase
{
    private AuditEvent()
    {
    }

    public AuditEvent(
        string action,
        string entityType,
        string entityId,
        string outcome,
        DateTimeOffset occurredAt,
        string? actorId = null,
        string? correlationId = null)
        : base(occurredAt)
    {
        Action = Require(action, nameof(action));
        EntityType = Require(entityType, nameof(entityType));
        EntityId = Require(entityId, nameof(entityId));
        Outcome = Require(outcome, nameof(outcome));
        OccurredAt = occurredAt;
        ActorId = Normalize(actorId);
        CorrelationId = Normalize(correlationId);
    }

    public DateTimeOffset OccurredAt { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Outcome { get; private set; } = string.Empty;
    public string? ActorId { get; private set; }
    public string? CorrelationId { get; private set; }

    private static string Require(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

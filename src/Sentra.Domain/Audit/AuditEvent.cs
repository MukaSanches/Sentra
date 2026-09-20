using Sentra.Domain.Common;

namespace Sentra.Domain.Audit;

public sealed class AuditEvent : EntityBase
{
    private AuditEvent() { }

    public AuditEvent(string action, string entityType, string entityId, string result, string? correlationId = null)
    {
        Action = Required(action, nameof(action), 128);
        EntityType = Required(entityType, nameof(entityType), 128);
        EntityId = Required(entityId, nameof(entityId), 128);
        Result = Required(result, nameof(result), 64);
        CorrelationId = Optional(correlationId, 128);
    }

    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Result { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }

    private static string Required(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("O valor é obrigatório.", name);
        value = value.Trim();
        if (value.Length > maxLength) throw new ArgumentOutOfRangeException(name, $"O valor excede {maxLength} caracteres.");
        return value;
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (value.Length > maxLength) throw new ArgumentOutOfRangeException(nameof(value), $"O valor excede {maxLength} caracteres.");
        return value;
    }
}

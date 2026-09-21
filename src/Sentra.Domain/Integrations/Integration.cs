using Sentra.Domain.Common;

namespace Sentra.Domain.Integrations;

public enum IntegrationKind
{
    WhatsApp = 0
}

public enum IntegrationStatus
{
    AwaitingConfiguration = 0,
    Connected = 1,
    Degraded = 2,
    Disabled = 3
}

public sealed class Integration : EntityBase
{
    private Integration()
    {
    }

    public Integration(
        Guid condominiumId,
        IntegrationKind kind,
        string externalResourceId,
        string? displayName)
    {
        if (condominiumId == Guid.Empty)
        {
            throw new ArgumentException("Condomínio inválido.", nameof(condominiumId));
        }

        CondominiumId = condominiumId;
        Kind = kind;
        ExternalResourceId = Required(externalResourceId, nameof(externalResourceId), 160);
        DisplayName = Optional(displayName, 160);
        Status = IntegrationStatus.AwaitingConfiguration;
    }

    public Guid CondominiumId { get; private set; }
    public IntegrationKind Kind { get; private set; }
    public string ExternalResourceId { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public IntegrationStatus Status { get; private set; }
    public DateTimeOffset? LastVerifiedAt { get; private set; }
    public string? LastErrorCode { get; private set; }

    public void MarkConnected(
        string? displayName,
        DateTimeOffset timestamp)
    {
        DisplayName = Optional(displayName, 160);
        Status = IntegrationStatus.Connected;
        LastVerifiedAt = timestamp;
        LastErrorCode = null;
        MarkUpdated(timestamp);
    }

    public void MarkDegraded(string errorCode, DateTimeOffset timestamp)
    {
        LastErrorCode = Required(errorCode, nameof(errorCode), 96);
        Status = IntegrationStatus.Degraded;
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

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        return normalized;
    }
}

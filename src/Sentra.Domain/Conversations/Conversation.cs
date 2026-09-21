using Sentra.Domain.Common;

namespace Sentra.Domain.Conversations;

public enum ConversationChannel
{
    WhatsApp = 0
}

public enum ConversationStatus
{
    Open = 0,
    Closed = 1
}

public sealed class Conversation : EntityBase
{
    private Conversation()
    {
    }

    public Conversation(
        Guid condominiumId,
        ConversationChannel channel,
        string externalParticipantId,
        string? displayName,
        Guid? residentId,
        Guid? unitId,
        DateTimeOffset openedAt)
        : base(openedAt)
    {
        if (condominiumId == Guid.Empty)
        {
            throw new ArgumentException("Condomínio inválido.", nameof(condominiumId));
        }

        CondominiumId = condominiumId;
        Channel = channel;
        ExternalParticipantId = Required(
            externalParticipantId,
            nameof(externalParticipantId),
            160);
        DisplayName = Optional(displayName, 160);
        ResidentId = residentId;
        UnitId = unitId;
        OpenedAt = openedAt;
        LastMessageAt = openedAt;
        Status = ConversationStatus.Open;
    }

    public Guid CondominiumId { get; private set; }
    public ConversationChannel Channel { get; private set; }
    public string ExternalParticipantId { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public Guid? ResidentId { get; private set; }
    public Guid? UnitId { get; private set; }
    public ConversationStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset LastMessageAt { get; private set; }

    public void RegisterMessage(DateTimeOffset timestamp)
    {
        if (timestamp > LastMessageAt)
        {
            LastMessageAt = timestamp;
        }

        MarkUpdated(timestamp > UpdatedAt ? timestamp : UpdatedAt);
    }

    public void LinkResident(
        Guid residentId,
        Guid? unitId,
        string? displayName,
        DateTimeOffset timestamp)
    {
        ResidentId = residentId;
        UnitId = unitId;
        DisplayName = Optional(displayName, 160);
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

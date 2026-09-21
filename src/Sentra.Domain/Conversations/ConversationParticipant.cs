using Sentra.Domain.Common;

namespace Sentra.Domain.Conversations;

public enum ConversationParticipantKind
{
    External = 0,
    Resident = 1,
    Employee = 2
}

public sealed class ConversationParticipant : EntityBase
{
    private ConversationParticipant()
    {
    }

    public ConversationParticipant(
        Guid conversationId,
        ConversationParticipantKind kind,
        string externalIdentifier,
        string? displayName,
        Guid? referenceId = null)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversa inválida.", nameof(conversationId));
        }

        ConversationId = conversationId;
        Kind = kind;
        ExternalIdentifier = Required(
            externalIdentifier,
            nameof(externalIdentifier),
            160);
        DisplayName = Optional(displayName, 160);
        ReferenceId = referenceId;
    }

    public Guid ConversationId { get; private set; }
    public ConversationParticipantKind Kind { get; private set; }
    public string ExternalIdentifier { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public Guid? ReferenceId { get; private set; }

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

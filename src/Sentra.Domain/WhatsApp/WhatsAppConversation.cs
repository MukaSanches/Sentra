using Sentra.Domain.Common;

namespace Sentra.Domain.WhatsApp;

public sealed class WhatsAppConversation : AuditedEntityBase
{
    private WhatsAppConversation() { }

    public WhatsAppConversation(
        Guid condominiumId,
        string externalParticipantId,
        string createdBy,
        DateTimeOffset createdAt,
        Guid? residentId = null,
        string? displayName = null)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        ExternalParticipantId = Guard.Required(externalParticipantId, nameof(externalParticipantId), 32);
        ResidentId = residentId;
        DisplayName = Guard.Optional(displayName, nameof(displayName), 160);
        LastMessageAt = createdAt;
    }

    public Guid CondominiumId { get; private set; }
    public string ExternalParticipantId { get; private set; } = string.Empty;
    public Guid? ResidentId { get; private set; }
    public string? DisplayName { get; private set; }
    public DateTimeOffset? LastInboundAt { get; private set; }
    public DateTimeOffset LastMessageAt { get; private set; }

    public bool IsInsideCustomerServiceWindow(DateTimeOffset now)
        => LastInboundAt.HasValue
            && now >= LastInboundAt.Value
            && now - LastInboundAt.Value <= TimeSpan.FromHours(24);

    public void RecordInbound(DateTimeOffset timestamp, Guid? residentId, string? displayName)
    {
        LastInboundAt = timestamp;
        if (timestamp > LastMessageAt) LastMessageAt = timestamp;
        if (residentId.HasValue) ResidentId = residentId;
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = Guard.Optional(displayName, nameof(displayName), 160);
        MarkUpdated(timestamp);
    }

    public void RecordOutbound(DateTimeOffset timestamp)
    {
        if (timestamp > LastMessageAt) LastMessageAt = timestamp;
        MarkUpdated(timestamp);
    }
}

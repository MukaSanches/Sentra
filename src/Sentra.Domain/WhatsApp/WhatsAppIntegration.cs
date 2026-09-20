using Sentra.Domain.Common;

namespace Sentra.Domain.WhatsApp;

public sealed class WhatsAppIntegration : AuditedEntityBase
{
    private WhatsAppIntegration() { }

    public WhatsAppIntegration(
        Guid condominiumId,
        string wabaId,
        string phoneNumberId,
        string createdBy,
        DateTimeOffset createdAt)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        WabaId = Guard.Required(wabaId, nameof(wabaId), 80);
        PhoneNumberId = Guard.Required(phoneNumberId, nameof(phoneNumberId), 80);
    }

    public Guid CondominiumId { get; private set; }
    public string WabaId { get; private set; } = string.Empty;
    public string PhoneNumberId { get; private set; } = string.Empty;
    public string? DisplayPhoneNumber { get; private set; }
    public string? VerifiedName { get; private set; }
    public string? QualityRating { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool IsAppSubscribed { get; private set; }
    public DateTimeOffset? LastValidatedAt { get; private set; }
    public DateTimeOffset? LastWebhookAt { get; private set; }

    public void RecordValidation(
        string? displayPhoneNumber,
        string? verifiedName,
        string? qualityRating,
        bool appSubscribed,
        string updatedBy,
        DateTimeOffset timestamp)
    {
        DisplayPhoneNumber = Guard.Optional(displayPhoneNumber, nameof(displayPhoneNumber), 60);
        VerifiedName = Guard.Optional(verifiedName, nameof(verifiedName), 160);
        QualityRating = Guard.Optional(qualityRating, nameof(qualityRating), 30);
        IsAppSubscribed = appSubscribed;
        IsEnabled = true;
        LastValidatedAt = timestamp;
        Touch(updatedBy, timestamp);
    }

    public void RecordWebhook(DateTimeOffset timestamp)
    {
        LastWebhookAt = timestamp;
        MarkUpdated(timestamp);
    }

    public void Disable(string updatedBy, DateTimeOffset timestamp)
    {
        IsEnabled = false;
        Touch(updatedBy, timestamp);
    }
}

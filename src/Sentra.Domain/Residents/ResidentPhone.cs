using Sentra.Domain.Common;

namespace Sentra.Domain.Residents;

public sealed class ResidentPhone : AuditedEntityBase
{
    private ResidentPhone()
    {
    }

    public ResidentPhone(
        Guid residentId,
        string e164Number,
        bool isPrimary,
        bool isWhatsAppEnabled,
        string createdBy,
        DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        ResidentId = Guard.RequiredId(residentId, nameof(residentId));
        E164Number = NormalizeE164(e164Number);
        IsPrimary = isPrimary;
        IsWhatsAppEnabled = isWhatsAppEnabled;
    }

    public Guid ResidentId { get; private set; }

    public string E164Number { get; private set; } = string.Empty;

    public bool IsPrimary { get; private set; }

    public bool IsWhatsAppEnabled { get; private set; }

    public void Update(
        string e164Number,
        bool isPrimary,
        bool isWhatsAppEnabled,
        string updatedBy,
        DateTimeOffset timestamp)
    {
        E164Number = NormalizeE164(e164Number);
        IsPrimary = isPrimary;
        IsWhatsAppEnabled = isWhatsAppEnabled;
        Touch(updatedBy, timestamp);
    }

    private static string NormalizeE164(string value)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Length is < 9 or > 16
            || normalized[0] != '+'
            || normalized.Skip(1).Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException("Phone number must be in E.164 format.", nameof(value));
        }

        return normalized;
    }
}

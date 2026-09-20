using Sentra.Domain.Common;

namespace Sentra.Domain.Residents;

public sealed class Resident : AuditedEntityBase
{
    private Resident()
    {
    }

    public Resident(
        Guid condominiumId,
        string fullName,
        string createdBy,
        DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        FullName = Guard.Required(fullName, nameof(fullName), 160);
        IsActive = true;
    }

    public Guid CondominiumId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void Rename(string fullName, string updatedBy, DateTimeOffset timestamp)
    {
        FullName = Guard.Required(fullName, nameof(fullName), 160);
        Touch(updatedBy, timestamp);
    }

    public void SetActive(bool isActive, string updatedBy, DateTimeOffset timestamp)
    {
        IsActive = isActive;
        Touch(updatedBy, timestamp);
    }
}

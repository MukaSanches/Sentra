using Sentra.Domain.Common;

namespace Sentra.Domain.Residents;

public sealed class Resident : AuditedEntityBase
{
    private Resident()
    {
    }

    public Resident(string fullName, string createdBy, DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        FullName = Guard.Required(fullName, nameof(fullName), 160);
        IsActive = true;
    }

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

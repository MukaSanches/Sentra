using Sentra.Domain.Common;

namespace Sentra.Domain.Access;

public sealed class Employee : AuditedEntityBase
{
    private Employee()
    {
    }

    public Employee(
        Guid condominiumId,
        string identitySubject,
        string fullName,
        string createdBy,
        DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        IdentitySubject = Guard.Required(identitySubject, nameof(identitySubject), 200);
        FullName = Guard.Required(fullName, nameof(fullName), 160);
        IsActive = true;
    }

    public Guid CondominiumId { get; private set; }

    public string IdentitySubject { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void UpdateName(string fullName, string updatedBy, DateTimeOffset timestamp)
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

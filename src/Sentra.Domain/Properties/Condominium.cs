using Sentra.Domain.Common;

namespace Sentra.Domain.Properties;

public sealed class Condominium : AuditedEntityBase
{
    private Condominium()
    {
    }

    public Condominium(string name, string createdBy, DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        Name = Guard.Required(name, nameof(name), 160);
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void Rename(string name, string updatedBy, DateTimeOffset timestamp)
    {
        Name = Guard.Required(name, nameof(name), 160);
        Touch(updatedBy, timestamp);
    }

    public void SetActive(bool isActive, string updatedBy, DateTimeOffset timestamp)
    {
        IsActive = isActive;
        Touch(updatedBy, timestamp);
    }
}

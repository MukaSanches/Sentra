using Sentra.Domain.Common;

namespace Sentra.Domain.Access;

public sealed class Role : AuditedEntityBase
{
    private Role()
    {
    }

    public Role(
        Guid condominiumId,
        string name,
        string createdBy,
        bool isSystem = false,
        DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        Name = Guard.Required(name, nameof(name), 100);
        IsSystem = isSystem;
    }

    public Guid CondominiumId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsSystem { get; private set; }

    public void Rename(string name, string updatedBy, DateTimeOffset timestamp)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System roles cannot be renamed.");
        }

        Name = Guard.Required(name, nameof(name), 100);
        Touch(updatedBy, timestamp);
    }
}

using Sentra.Domain.Common;

namespace Sentra.Domain.Properties;

public sealed class Block : AuditedEntityBase
{
    private Block()
    {
    }

    public Block(
        Guid condominiumId,
        string name,
        string createdBy,
        string? code = null,
        DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        Name = Guard.Required(name, nameof(name), 120);
        Code = Guard.Optional(code, nameof(code), 40);
        IsActive = true;
    }

    public Guid CondominiumId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Code { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(string name, string? code, string updatedBy, DateTimeOffset timestamp)
    {
        Name = Guard.Required(name, nameof(name), 120);
        Code = Guard.Optional(code, nameof(code), 40);
        Touch(updatedBy, timestamp);
    }

    public void SetActive(bool isActive, string updatedBy, DateTimeOffset timestamp)
    {
        IsActive = isActive;
        Touch(updatedBy, timestamp);
    }
}

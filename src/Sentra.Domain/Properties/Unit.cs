using Sentra.Domain.Common;

namespace Sentra.Domain.Properties;

public sealed class Unit : AuditedEntityBase
{
    private Unit()
    {
    }

    public Unit(
        Guid condominiumId,
        Guid blockId,
        string number,
        string createdBy,
        string? floor = null,
        DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        BlockId = Guard.RequiredId(blockId, nameof(blockId));
        Number = Guard.Required(number, nameof(number), 40);
        Floor = Guard.Optional(floor, nameof(floor), 40);
        IsActive = true;
    }

    public Guid CondominiumId { get; private set; }

    public Guid BlockId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public string? Floor { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(string number, string? floor, string updatedBy, DateTimeOffset timestamp)
    {
        Number = Guard.Required(number, nameof(number), 40);
        Floor = Guard.Optional(floor, nameof(floor), 40);
        Touch(updatedBy, timestamp);
    }

    public void SetActive(bool isActive, string updatedBy, DateTimeOffset timestamp)
    {
        IsActive = isActive;
        Touch(updatedBy, timestamp);
    }
}

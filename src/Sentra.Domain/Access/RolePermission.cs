using Sentra.Domain.Common;

namespace Sentra.Domain.Access;

public sealed class RolePermission : AuditedEntityBase
{
    private RolePermission()
    {
    }

    public RolePermission(
        Guid condominiumId,
        Guid roleId,
        Guid permissionId,
        string createdBy,
        DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        RoleId = Guard.RequiredId(roleId, nameof(roleId));
        PermissionId = Guard.RequiredId(permissionId, nameof(permissionId));
    }

    public Guid CondominiumId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }
}

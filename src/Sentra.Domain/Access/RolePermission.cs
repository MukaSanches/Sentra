using Sentra.Domain.Common;

namespace Sentra.Domain.Access;

public sealed class RolePermission : AuditedEntityBase
{
    private RolePermission()
    {
    }

    public RolePermission(Guid roleId, Guid permissionId, string createdBy, DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        RoleId = Guard.RequiredId(roleId, nameof(roleId));
        PermissionId = Guard.RequiredId(permissionId, nameof(permissionId));
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }
}

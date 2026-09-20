using Sentra.Domain.Common;

namespace Sentra.Domain.Access;

public sealed class EmployeeRole : AuditedEntityBase
{
    private EmployeeRole()
    {
    }

    public EmployeeRole(
        Guid condominiumId,
        Guid employeeId,
        Guid roleId,
        string createdBy,
        DateTimeOffset? createdAt = null)
        : base(createdBy, createdAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        EmployeeId = Guard.RequiredId(employeeId, nameof(employeeId));
        RoleId = Guard.RequiredId(roleId, nameof(roleId));
    }

    public Guid CondominiumId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public Guid RoleId { get; private set; }
}

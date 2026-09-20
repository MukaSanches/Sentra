using Microsoft.EntityFrameworkCore;
using Sentra.Application.Security;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Infrastructure.Security;

public sealed class PermissionEvaluator(SentraDbContext dbContext) : IPermissionEvaluator
{
    public Task<bool> HasPermissionAsync(
        string identitySubject,
        Guid condominiumId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identitySubject) || condominiumId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode))
        {
            return Task.FromResult(false);
        }

        var query =
            from employee in dbContext.Employees.AsNoTracking()
            join employeeRole in dbContext.EmployeeRoles.AsNoTracking()
                on employee.Id equals employeeRole.EmployeeId
            join rolePermission in dbContext.RolePermissions.AsNoTracking()
                on employeeRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions.AsNoTracking()
                on rolePermission.PermissionId equals permission.Id
            where employee.CondominiumId == condominiumId
                && employeeRole.CondominiumId == condominiumId
                && rolePermission.CondominiumId == condominiumId
                && employee.IdentitySubject == identitySubject
                && employee.IsActive
                && permission.Code == permissionCode
            select permission.Id;

        return query.AnyAsync(cancellationToken);
    }
}

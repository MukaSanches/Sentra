using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Security;
using Sentra.Contracts.Admin;
using Sentra.Domain.Security;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapSentraAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin");

        group.MapGet("/permissions", PermissionsAsync)
            .RequireAuthorization(PermissionCatalog.EmployeesManage);
        group.MapGet("/roles", RolesAsync)
            .RequireAuthorization(PermissionCatalog.EmployeesRead);
        group.MapPost("/roles", CreateRoleAsync)
            .RequireAuthorization(PermissionCatalog.EmployeesManage);
        group.MapGet("/employees", EmployeesAsync)
            .RequireAuthorization(PermissionCatalog.EmployeesRead);
        group.MapPost("/employees", CreateEmployeeAsync)
            .RequireAuthorization(PermissionCatalog.EmployeesManage);

        return endpoints;
    }

    private static async Task<IResult> PermissionsAsync(
        SentraDbContext db,
        CancellationToken ct)
    {
        var rows = await db.Permissions.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new PermissionAdminResponse(x.Code, x.Description))
            .ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> RolesAsync(
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var condominiumId)) return Results.Unauthorized();

        var roles = await db.Roles.AsNoTracking()
            .Where(x => x.CondominiumId == condominiumId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        var roleIds = roles.Select(x => x.Id).ToArray();
        var permissions = await (
            from link in db.RolePermissions.AsNoTracking()
            join permission in db.Permissions.AsNoTracking() on link.PermissionId equals permission.Id
            where roleIds.Contains(link.RoleId)
            select new { link.RoleId, permission.Code })
            .ToListAsync(ct);

        return Results.Ok(roles.Select(role => new RoleAdminResponse(
            role.Id,
            role.Name,
            role.Description,
            permissions.Where(x => x.RoleId == role.Id).Select(x => x.Code).OrderBy(x => x).ToArray())));
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateRoleAdminRequest request,
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var condominiumId)) return Results.Unauthorized();

        var codes = request.PermissionCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var permissions = await db.Permissions
            .Where(x => codes.Contains(x.Code))
            .ToListAsync(ct);

        if (permissions.Count != codes.Length)
        {
            return Results.BadRequest(new { error = "Uma ou mais permissões não existem." });
        }

        var role = new Role(condominiumId, request.Name, request.Description);
        db.Roles.Add(role);
        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission(role.Id, permission.Id));
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (PostgresErrorClassifier.IsUniqueViolation(e))
        {
            return Results.Conflict(new { error = "Já existe um perfil com este nome." });
        }

        return Results.Ok(new RoleAdminResponse(
            role.Id, role.Name, role.Description, permissions.Select(x => x.Code).OrderBy(x => x).ToArray()));
    }

    private static async Task<IResult> EmployeesAsync(
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var condominiumId)) return Results.Unauthorized();

        var rows = await (
            from employee in db.Employees.AsNoTracking()
            join role in db.Roles.AsNoTracking() on employee.RoleId equals role.Id
            where employee.CondominiumId == condominiumId
            orderby employee.FullName
            select new EmployeeAdminResponse(
                employee.Id,
                employee.FullName,
                employee.Username,
                employee.RoleId,
                role.Name,
                employee.IsActive,
                employee.LockedUntil))
            .ToListAsync(ct);

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateEmployeeAsync(
        CreateEmployeeAdminRequest request,
        ClaimsPrincipal user,
        SentraDbContext db,
        IPasswordHashService passwordHash,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var condominiumId)) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
        {
            return Results.BadRequest(new { error = "A senha deve possuir pelo menos 12 caracteres." });
        }

        var role = await db.Roles.SingleOrDefaultAsync(
            x => x.Id == request.RoleId && x.CondominiumId == condominiumId, ct);
        if (role is null)
        {
            return Results.BadRequest(new { error = "Perfil não pertence ao condomínio." });
        }

        var employee = new Employee(
            condominiumId,
            role.Id,
            request.FullName,
            request.Username,
            passwordHash.Hash(request.Password));

        db.Employees.Add(employee);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (PostgresErrorClassifier.IsUniqueViolation(e))
        {
            return Results.Conflict(new { error = "Já existe um usuário com este login." });
        }

        return Results.Ok(new EmployeeAdminResponse(
            employee.Id, employee.FullName, employee.Username, role.Id, role.Name, employee.IsActive, employee.LockedUntil));
    }

    private static bool TryCondominium(ClaimsPrincipal user, out Guid condominiumId)
        => Guid.TryParse(user.FindFirst("condominium_id")?.Value, out condominiumId);
}

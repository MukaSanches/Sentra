namespace Sentra.Contracts.Admin;

public sealed record PermissionAdminResponse(string Code, string Description);

public sealed record RoleAdminResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions);

public sealed record CreateRoleAdminRequest(
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionCodes);

public sealed record EmployeeAdminResponse(
    Guid Id,
    string FullName,
    string Username,
    Guid RoleId,
    string RoleName,
    bool Active,
    DateTimeOffset? LockedUntil);

public sealed record CreateEmployeeAdminRequest(
    string FullName,
    string Username,
    string Password,
    Guid RoleId);

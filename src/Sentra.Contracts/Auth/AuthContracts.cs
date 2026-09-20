namespace Sentra.Contracts.Auth;

public sealed record LoginRequest(
    Guid CondominiumId,
    string Username,
    string Password);

public sealed record EmployeeIdentityResponse(
    Guid Id,
    string FullName,
    string Username,
    Guid CondominiumId,
    Guid RoleId);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    EmployeeIdentityResponse Employee,
    IReadOnlyList<string> Permissions);

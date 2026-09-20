namespace Sentra.Contracts.Operations;

public sealed record SetupStatusResponse(bool IsInitialized);

public sealed record BootstrapRequest(string CondominiumName, string AdministratorName);

public sealed record CondominiumResponse(Guid Id, string Name, bool IsActive);

public sealed record UpdateCondominiumRequest(string Name, bool IsActive);

public sealed record CreateBlockRequest(string Name, string? Code);

public sealed record UpdateBlockRequest(string Name, string? Code, bool IsActive);

public sealed record BlockResponse(Guid Id, Guid CondominiumId, string Name, string? Code, bool IsActive);

public sealed record CreateUnitRequest(Guid BlockId, string Number, string? Floor);

public sealed record UpdateUnitRequest(string Number, string? Floor, bool IsActive);

public sealed record UnitResponse(
    Guid Id,
    Guid CondominiumId,
    Guid BlockId,
    string Number,
    string? Floor,
    bool IsActive);

public sealed record CreateResidentRequest(string FullName);

public sealed record UpdateResidentRequest(string FullName, bool IsActive);

public sealed record ResidentResponse(Guid Id, Guid CondominiumId, string FullName, bool IsActive);

public sealed record AddResidentPhoneRequest(string E164Number, bool IsPrimary, bool IsWhatsAppEnabled);

public sealed record ResidentPhoneResponse(
    Guid Id,
    Guid ResidentId,
    string E164Number,
    bool IsPrimary,
    bool IsWhatsAppEnabled);

public sealed record LinkResidentUnitRequest(Guid UnitId, int Role, bool IsPrimary, DateTimeOffset StartsAt);

public sealed record ResidentUnitResponse(
    Guid Id,
    Guid ResidentId,
    Guid UnitId,
    int Role,
    bool IsPrimary,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt);

public sealed record EmployeeResponse(Guid Id, Guid CondominiumId, string FullName, string IdentitySubject, bool IsActive);

public sealed record RoleResponse(Guid Id, Guid CondominiumId, string Name, bool IsSystem);

public sealed record CreateRoleRequest(string Name);

public sealed record SetRolePermissionsRequest(IReadOnlyList<string> PermissionCodes);

public sealed record AssignEmployeeRoleRequest(Guid RoleId);

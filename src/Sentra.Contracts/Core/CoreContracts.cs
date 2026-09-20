namespace Sentra.Contracts.Core;

public sealed record CreateBlockRequest(string Name);

public sealed record BlockResponse(Guid Id, string Name);

public sealed record CreateUnitRequest(
    string Identifier,
    string DisplayName,
    Guid? BlockId);

public sealed record UnitResponse(
    Guid Id,
    string Identifier,
    string DisplayName,
    Guid? BlockId);

public sealed record CreateResidentRequest(
    string FullName,
    string PhoneE164,
    Guid UnitId,
    int Role);

public sealed record ResidentResponse(
    Guid Id,
    string FullName,
    string PhoneE164,
    Guid UnitId,
    string Unit,
    int Role,
    bool IsPrimary);

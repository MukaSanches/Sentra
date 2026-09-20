using Sentra.Contracts.Common;
using Sentra.Contracts.Operations;

namespace Sentra.Application.Operations;

public interface IOperationalDataService
{
    Task<SetupStatusResponse> GetSetupStatusAsync(CancellationToken cancellationToken);

    Task<CondominiumResponse> BootstrapAsync(
        BootstrapRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<CondominiumResponse?> GetCondominiumAsync(Guid condominiumId, CancellationToken cancellationToken);

    Task<CondominiumResponse?> UpdateCondominiumAsync(
        Guid condominiumId,
        UpdateCondominiumRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<PagedResponse<BlockResponse>> ListBlocksAsync(
        Guid condominiumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BlockResponse> CreateBlockAsync(
        Guid condominiumId,
        CreateBlockRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<BlockResponse?> UpdateBlockAsync(
        Guid condominiumId,
        Guid blockId,
        UpdateBlockRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<PagedResponse<UnitResponse>> ListUnitsAsync(
        Guid condominiumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<UnitResponse> CreateUnitAsync(
        Guid condominiumId,
        CreateUnitRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<UnitResponse?> UpdateUnitAsync(
        Guid condominiumId,
        Guid unitId,
        UpdateUnitRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<PagedResponse<ResidentResponse>> ListResidentsAsync(
        Guid condominiumId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ResidentResponse> CreateResidentAsync(
        Guid condominiumId,
        CreateResidentRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<ResidentResponse?> UpdateResidentAsync(
        Guid condominiumId,
        Guid residentId,
        UpdateResidentRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<ResidentPhoneResponse?> AddResidentPhoneAsync(
        Guid condominiumId,
        Guid residentId,
        AddResidentPhoneRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<ResidentUnitResponse?> LinkResidentUnitAsync(
        Guid condominiumId,
        Guid residentId,
        LinkResidentUnitRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<PagedResponse<EmployeeResponse>> ListEmployeesAsync(
        Guid condominiumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleResponse>> ListRolesAsync(
        Guid condominiumId,
        CancellationToken cancellationToken);

    Task<RoleResponse> CreateRoleAsync(
        Guid condominiumId,
        CreateRoleRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<bool> SetRolePermissionsAsync(
        Guid condominiumId,
        Guid roleId,
        SetRolePermissionsRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<bool> AssignEmployeeRoleAsync(
        Guid condominiumId,
        Guid employeeId,
        AssignEmployeeRoleRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken);
}

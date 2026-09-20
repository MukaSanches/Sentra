using Sentra.Contracts.Common;
using Sentra.Contracts.Operations;

namespace Sentra.Desktop.Services;

public interface ISentraApiClient
{
    Task<bool> IsServerAliveAsync(CancellationToken cancellationToken = default);

    Task<SetupStatusResponse> GetSetupStatusAsync(CancellationToken cancellationToken = default);

    Task<PagedResponse<BlockResponse>> GetBlocksAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<BlockResponse> CreateBlockAsync(CreateBlockRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<UnitResponse>> GetUnitsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<UnitResponse> CreateUnitAsync(CreateUnitRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<ResidentResponse>> GetResidentsAsync(string? search = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<ResidentResponse> CreateResidentAsync(CreateResidentRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<EmployeeResponse>> GetEmployeesAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
}

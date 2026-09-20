using Sentra.Contracts.Common;
using Sentra.Contracts.Operations;
using Sentra.Contracts.WhatsApp;

namespace Sentra.Desktop.Services;

public interface ISentraApiClient
{
    Task<bool> IsServerAliveAsync(CancellationToken cancellationToken = default);
    Task<SetupStatusResponse> GetSetupStatusAsync(CancellationToken cancellationToken = default);
    Task<CondominiumResponse> BootstrapAsync(BootstrapRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<BlockResponse>> GetBlocksAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<BlockResponse> CreateBlockAsync(CreateBlockRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<UnitResponse>> GetUnitsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<UnitResponse> CreateUnitAsync(CreateUnitRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ResidentResponse>> GetResidentsAsync(string? search = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<ResidentResponse> CreateResidentAsync(CreateResidentRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<EmployeeResponse>> GetEmployeesAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task SetRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default);
    Task AssignEmployeeRoleAsync(Guid employeeId, Guid roleId, CancellationToken cancellationToken = default);

    Task<WhatsAppConfigurationStatusResponse> GetWhatsAppStatusAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppConfigurationStatusResponse> ConfigureWhatsAppAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WhatsAppTemplateResponse>> GetWhatsAppTemplatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WhatsAppFlowResponse>> GetWhatsAppFlowsAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<WhatsAppConversationResponse>> GetWhatsAppConversationsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<PagedResponse<WhatsAppMessageResponse>> GetWhatsAppMessagesAsync(Guid conversationId, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    Task<WhatsAppSendResult> SendWhatsAppTextAsync(SendWhatsAppTextRequest request, CancellationToken cancellationToken = default);
    Task<WhatsAppSendResult> SendWhatsAppTemplateAsync(SendWhatsAppTemplateRequest request, CancellationToken cancellationToken = default);
}

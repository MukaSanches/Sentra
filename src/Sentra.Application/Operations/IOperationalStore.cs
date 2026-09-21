using Sentra.Contracts.Operations;

namespace Sentra.Application.Operations;

public interface IOperationalStore
{
    Task<IReadOnlyList<VisitorAuthorizationResponse>> ListVisitorAuthorizationsAsync(Guid condominiumId, bool activeOnly, CancellationToken cancellationToken);
    Task<VisitorAuthorizationResponse> CreateVisitorAuthorizationAsync(Guid condominiumId, CreateVisitorAuthorizationRequest request, CancellationToken cancellationToken);
    Task<VisitorAuthorizationResponse?> GetVisitorAuthorizationAsync(Guid condominiumId, Guid authorizationId, CancellationToken cancellationToken);
    Task<VisitorAuthorizationResponse?> RegisterVisitAsync(Guid condominiumId, Guid authorizationId, Guid actorId, string action, CancellationToken cancellationToken);
    Task<(Guid Id, string Payload, DateTimeOffset ExpiresAt, bool OneTime)> CreateQrCredentialAsync(Guid condominiumId, Guid authorizationId, bool oneTime, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<VisitorAuthorizationResponse?> ValidateQrAsync(Guid condominiumId, string payload, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceProviderResponse>> ListProvidersAsync(Guid condominiumId, CancellationToken cancellationToken);
    Task<ServiceProviderResponse> CreateProviderAsync(Guid condominiumId, CreateServiceProviderRequest request, CancellationToken cancellationToken);
    Task<ProviderAuthorizationResponse> CreateProviderAuthorizationAsync(Guid condominiumId, CreateProviderAuthorizationRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<PackageResponse>> ListPackagesAsync(Guid condominiumId, bool pendingOnly, CancellationToken cancellationToken);
    Task<PackageResponse> CreatePackageAsync(Guid condominiumId, Guid actorId, CreatePackageRequest request, CancellationToken cancellationToken);
    Task<PackageResponse?> CollectPackageAsync(Guid condominiumId, Guid packageId, Guid actorId, string? pickupCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<OccurrenceResponse>> ListOccurrencesAsync(Guid condominiumId, bool openOnly, CancellationToken cancellationToken);
    Task<OccurrenceResponse> CreateOccurrenceAsync(Guid condominiumId, CreateOccurrenceRequest request, CancellationToken cancellationToken);
    Task<OccurrenceResponse?> UpdateOccurrenceAsync(Guid condominiumId, Guid occurrenceId, UpdateOccurrenceRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ShiftResponse>> ListShiftsAsync(Guid condominiumId, CancellationToken cancellationToken);
    Task<ShiftResponse> OpenShiftAsync(Guid condominiumId, Guid employeeId, CancellationToken cancellationToken);
    Task<ShiftResponse?> CloseShiftAsync(Guid condominiumId, Guid shiftId, Guid employeeId, string summary, CancellationToken cancellationToken);
    Task<ShiftResponse?> AcknowledgeShiftAsync(Guid condominiumId, Guid shiftId, Guid employeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AnnouncementResponse>> ListAnnouncementsAsync(Guid condominiumId, CancellationToken cancellationToken);
    Task<AnnouncementResponse> CreateAnnouncementAsync(Guid condominiumId, CreateAnnouncementRequest request, CancellationToken cancellationToken);

    Task<Guid> SaveInterpretationAsync(Guid condominiumId, Guid conversationId, string intent, string resolutionState, string summary, string extractedJson, string missingJson, CancellationToken cancellationToken);
    Task<Guid> CreatePendingActionAsync(Guid condominiumId, Guid conversationId, Guid interpretationId, string actionType, string riskLevel, string payloadJson, CancellationToken cancellationToken);
    Task<PendingActionResponse?> GetPendingActionAsync(Guid condominiumId, Guid pendingActionId, CancellationToken cancellationToken);
    Task<PendingActionResponse?> ResolvePendingActionAsync(Guid condominiumId, Guid pendingActionId, Guid employeeId, string decision, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultResponse>> SearchAsync(Guid condominiumId, string query, CancellationToken cancellationToken);
    Task<OfflineSnapshotResponse> GetOfflineSnapshotAsync(Guid condominiumId, CancellationToken cancellationToken);
    Task<DashboardResponse> GetDashboardAsync(Guid condominiumId, Guid? employeeId, CancellationToken cancellationToken);
}

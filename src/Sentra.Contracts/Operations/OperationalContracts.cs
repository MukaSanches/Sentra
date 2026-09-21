namespace Sentra.Contracts.Operations;

public sealed record VisitorAuthorizationResponse(
    Guid Id,
    Guid UnitId,
    Guid? ResidentId,
    string VisitorName,
    string? Document,
    string? Relationship,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VehiclePlate,
    string? Purpose,
    string Status,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CheckedOutAt);

public sealed record CreateVisitorAuthorizationRequest(
    Guid UnitId,
    Guid? ResidentId,
    string VisitorName,
    string? Document,
    string? Relationship,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VehiclePlate,
    string? Purpose);

public sealed record RegisterVisitRequest(string Action);

public sealed record QrCredentialResponse(
    Guid Id,
    Guid AuthorizationId,
    string Payload,
    string Svg,
    DateTimeOffset ExpiresAt,
    bool OneTime);

public sealed record ValidateQrRequest(string Payload);

public sealed record ValidateQrResponse(
    bool Valid,
    string Status,
    VisitorAuthorizationResponse? Authorization);

public sealed record ServiceProviderResponse(
    Guid Id,
    string Name,
    string? Document,
    string? Company,
    bool Active);

public sealed record CreateServiceProviderRequest(
    string Name,
    string? Document,
    string? Company);

public sealed record ProviderAuthorizationResponse(
    Guid Id,
    Guid ProviderId,
    Guid? UnitId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Purpose,
    string Status);

public sealed record CreateProviderAuthorizationRequest(
    Guid ProviderId,
    Guid? UnitId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Purpose);

public sealed record PackageResponse(
    Guid Id,
    Guid UnitId,
    string Carrier,
    string? Description,
    DateTimeOffset ReceivedAt,
    string Status,
    DateTimeOffset? PickedUpAt);

public sealed record CreatePackageRequest(
    Guid UnitId,
    string Carrier,
    string? Description);

public sealed record CollectPackageRequest(string? PickupCode);

public sealed record OccurrenceResponse(
    Guid Id,
    string Category,
    string Title,
    string Description,
    string Priority,
    string Status,
    Guid? UnitId,
    Guid? OwnerEmployeeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateOccurrenceRequest(
    string Category,
    string Title,
    string Description,
    string Priority,
    Guid? UnitId);

public sealed record UpdateOccurrenceRequest(
    string Status,
    Guid? OwnerEmployeeId);

public sealed record ShiftResponse(
    Guid Id,
    Guid EmployeeId,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    string? HandoffSummary,
    Guid? AcknowledgedBy,
    DateTimeOffset? AcknowledgedAt);

public sealed record OpenShiftRequest();

public sealed record CloseShiftRequest(string HandoffSummary);

public sealed record AcknowledgeShiftRequest();

public sealed record AnnouncementResponse(
    Guid Id,
    string Title,
    string Body,
    string Audience,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    bool Active);

public sealed record CreateAnnouncementRequest(
    string Title,
    string Body,
    string Audience,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt);

public sealed record SearchResultResponse(
    string Kind,
    Guid Id,
    string Title,
    string Subtitle,
    DateTimeOffset? Timestamp);

public sealed record IntelligenceAnalysisResponse(
    Guid InterpretationId,
    Guid? PendingActionId,
    string Intent,
    string ResolutionState,
    string Summary,
    IReadOnlyDictionary<string, string?> Extracted,
    IReadOnlyList<string> MissingFields,
    string RiskLevel);

public sealed record PendingActionResponse(
    Guid Id,
    Guid ConversationId,
    string ActionType,
    string RiskLevel,
    string Status,
    string PayloadJson,
    DateTimeOffset CreatedAt);

public sealed record ResolvePendingActionRequest(string Decision);

public sealed record OfflineResidentResponse(
    Guid Id,
    string FullName,
    string PhoneE164,
    Guid UnitId,
    string UnitDisplayName);

public sealed record OfflineSnapshotResponse(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<OfflineResidentResponse> Residents,
    IReadOnlyList<VisitorAuthorizationResponse> ActiveAuthorizations,
    IReadOnlyList<PackageResponse> PendingPackages,
    IReadOnlyList<ServiceProviderResponse> Providers);

public sealed record DashboardResponse(
    int VisitorsInside,
    int PendingPackages,
    int OpenOccurrences,
    int ActiveAuthorizations,
    int PendingActions,
    Guid? CurrentShiftId);

public sealed record AuditRecordResponse(
    Guid Id,
    string Action,
    string EntityType,
    string EntityId,
    string Outcome,
    DateTimeOffset OccurredAt,
    string? ActorId,
    string? CorrelationId);

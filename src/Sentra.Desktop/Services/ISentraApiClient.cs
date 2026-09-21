using System.IO;
using Sentra.Contracts.Auth;\nusing Sentra.Contracts.Core;\nusing Sentra.Contracts.Operations;
using Sentra.Contracts.Setup;
using Sentra.Contracts.WhatsApp;

namespace Sentra.Desktop.Services;

public interface ISentraApiClient
{
    Task<bool> IsAliveAsync(CancellationToken cancellationToken = default);
    Task<SetupStatusResponse> GetSetupStatusAsync(CancellationToken cancellationToken = default);
    Task<BootstrapResponse> BootstrapAsync(
        BootstrapRequest request,
        string bootstrapToken,
        CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<WhatsAppConfigurationStatusResponse> GetWhatsAppConfigurationAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitResponse>> GetUnitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResidentResponse>> GetResidentsAsync(CancellationToken cancellationToken = default);

    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VisitorAuthorizationResponse>> GetVisitorAuthorizationsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<VisitorAuthorizationResponse> CreateVisitorAuthorizationAsync(CreateVisitorAuthorizationRequest request, CancellationToken cancellationToken = default);
    Task<VisitorAuthorizationResponse> RegisterVisitAsync(Guid authorizationId, string action, CancellationToken cancellationToken = default);
    Task<QrCredentialResponse> CreateQrAsync(Guid authorizationId, CancellationToken cancellationToken = default);
    Task<ValidateQrResponse> ValidateQrAsync(string payload, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceProviderResponse>> GetProvidersAsync(CancellationToken cancellationToken = default);
    Task<ServiceProviderResponse> CreateProviderAsync(CreateServiceProviderRequest request, CancellationToken cancellationToken = default);
    Task<ProviderAuthorizationResponse> CreateProviderAuthorizationAsync(CreateProviderAuthorizationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageResponse>> GetPackagesAsync(bool pendingOnly = true, CancellationToken cancellationToken = default);
    Task<PackageResponse> CreatePackageAsync(CreatePackageRequest request, CancellationToken cancellationToken = default);
    Task<PackageResponse> CollectPackageAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OccurrenceResponse>> GetOccurrencesAsync(bool openOnly = true, CancellationToken cancellationToken = default);
    Task<OccurrenceResponse> CreateOccurrenceAsync(CreateOccurrenceRequest request, CancellationToken cancellationToken = default);
    Task<OccurrenceResponse> UpdateOccurrenceAsync(Guid occurrenceId, UpdateOccurrenceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftResponse>> GetShiftsAsync(CancellationToken cancellationToken = default);
    Task<ShiftResponse> OpenShiftAsync(CancellationToken cancellationToken = default);
    Task<ShiftResponse> CloseShiftAsync(Guid shiftId, string summary, CancellationToken cancellationToken = default);
    Task<ShiftResponse> AcknowledgeShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnnouncementResponse>> GetAnnouncementsAsync(CancellationToken cancellationToken = default);
    Task<AnnouncementResponse> CreateAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchResultResponse>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<OfflineSnapshotResponse> GetOfflineSnapshotAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditRecordResponse>> GetAuditAsync(int take = 200, CancellationToken cancellationToken = default);
    Task<IntelligenceAnalysisResponse> AnalyzeConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<PendingActionResponse> ResolvePendingActionAsync(Guid pendingActionId, string decision, CancellationToken cancellationToken = default);
    Task SendOutboxAsync(string method, string path, string? jsonBody, CancellationToken cancellationToken = default);

    Task<WhatsAppIntegrationStatusResponse> GetWhatsAppStatusAsync(
        CancellationToken cancellationToken = default);
    Task<WhatsAppIntegrationVerifyResponse> VerifyWhatsAppAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WhatsAppTemplateResponse>> GetTemplatesAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WhatsAppFlowResponse>> GetFlowsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummaryResponse>> GetConversationsAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationMessageResponse>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationAttachmentResponse>> GetAttachmentsAsync(
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task<ConversationMessageResponse> SendTextAsync(
        Guid conversationId,
        string text,
        CancellationToken cancellationToken = default);
    Task<ConversationMessageResponse> SendTemplateAsync(
        Guid conversationId,
        SendWhatsAppTemplateRequest request,
        CancellationToken cancellationToken = default);
    Task<ConversationMessageResponse> SendButtonsAsync(
        Guid conversationId,
        SendWhatsAppReplyButtonsRequest request,
        CancellationToken cancellationToken = default);
    Task<ConversationMessageResponse> SendFlowAsync(
        Guid conversationId,
        SendWhatsAppFlowRequest request,
        CancellationToken cancellationToken = default);
    Task<ConversationMessageResponse> SendMediaAsync(
        Guid conversationId,
        string kind,
        Stream content,
        string fileName,
        string contentType,
        string? caption,
        CancellationToken cancellationToken = default);
    Task MarkReadAsync(
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken = default);
    Task DownloadAttachmentAsync(
        Guid conversationId,
        Guid attachmentId,
        Stream destination,
        CancellationToken cancellationToken = default);
}

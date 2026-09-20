using Sentra.Contracts.Auth;
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

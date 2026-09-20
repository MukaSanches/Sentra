using Sentra.Contracts.Common;
using Sentra.Contracts.WhatsApp;

namespace Sentra.WhatsApp.Services;

public interface IWhatsAppMessagingService
{
    Task<PagedResponse<WhatsAppConversationResponse>> ListConversationsAsync(
        Guid condominiumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResponse<WhatsAppMessageResponse>> ListMessagesAsync(
        Guid condominiumId,
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendTextAsync(
        Guid condominiumId,
        SendWhatsAppTextRequest request,
        string actor,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendButtonsAsync(
        Guid condominiumId,
        SendWhatsAppButtonsRequest request,
        string actor,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendListAsync(
        Guid condominiumId,
        SendWhatsAppListRequest request,
        string actor,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendTemplateAsync(
        Guid condominiumId,
        SendWhatsAppTemplateRequest request,
        string actor,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendMediaAsync(
        Guid condominiumId,
        SendWhatsAppMediaRequest request,
        string actor,
        CancellationToken cancellationToken);

    Task<WhatsAppSendResult> SendFlowAsync(
        Guid condominiumId,
        SendWhatsAppFlowRequest request,
        string actor,
        CancellationToken cancellationToken);

    Task<string> UploadMediaAsync(
        Stream stream,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken);
}

public sealed class CustomerServiceWindowClosedException()
    : InvalidOperationException("A janela de atendimento de 24 horas está fechada. Use um template aprovado.");

using Sentra.Contracts.Common;

namespace Sentra.Contracts.WhatsApp;

public sealed record WhatsAppConfigurationStatusResponse(
    bool ConfigurationPresent,
    bool ApiReachable,
    bool WabaValidated,
    bool PhoneValidated,
    bool AppSubscribed,
    string? DisplayPhoneNumber,
    string? VerifiedName,
    string? QualityRating,
    int ApprovedTemplates,
    int Flows,
    DateTimeOffset? LastValidatedAt,
    DateTimeOffset? LastWebhookAt,
    string WebhookCallbackPath,
    IReadOnlyList<string> MissingConfiguration,
    string State);

public sealed record WhatsAppPhoneNumberResponse(
    string Id,
    string? DisplayPhoneNumber,
    string? VerifiedName,
    string? QualityRating);

public sealed record WhatsAppTemplateResponse(
    string Id,
    string Name,
    string Language,
    string Status,
    string Category);

public sealed record WhatsAppFlowResponse(string Id, string Name, string Status);

public sealed record WhatsAppConversationResponse(
    Guid Id,
    Guid CondominiumId,
    string ExternalParticipantId,
    Guid? ResidentId,
    string? DisplayName,
    DateTimeOffset? LastInboundAt,
    DateTimeOffset LastMessageAt,
    bool CustomerServiceWindowOpen);

public sealed record WhatsAppMessageResponse(
    Guid Id,
    Guid ConversationId,
    string? ExternalMessageId,
    int Direction,
    int Type,
    string? Text,
    DateTimeOffset MessageTimestamp,
    int DeliveryStatus,
    DateTimeOffset DeliveryStatusAt,
    string? ErrorTitle);

public sealed record SendWhatsAppTextRequest(string To, string Text, string? ReplyToMessageId = null);

public sealed record WhatsAppReplyButton(string Id, string Title);

public sealed record SendWhatsAppButtonsRequest(
    string To,
    string Body,
    IReadOnlyList<WhatsAppReplyButton> Buttons);

public sealed record WhatsAppListRow(string Id, string Title, string? Description);

public sealed record WhatsAppListSection(string Title, IReadOnlyList<WhatsAppListRow> Rows);

public sealed record SendWhatsAppListRequest(
    string To,
    string Body,
    string ButtonText,
    IReadOnlyList<WhatsAppListSection> Sections);

public sealed record SendWhatsAppTemplateRequest(
    string To,
    string TemplateName,
    string LanguageCode,
    IReadOnlyList<string>? BodyParameters = null);

public sealed record SendWhatsAppMediaRequest(
    string To,
    string MediaId,
    string MediaType,
    string? Caption = null,
    string? FileName = null);

public sealed record SendWhatsAppFlowRequest(
    string To,
    string Body,
    string FlowId,
    string FlowToken,
    string ButtonText,
    string FlowAction,
    string? Screen = null,
    string? ActionDataJson = null);

public sealed record WhatsAppSendResult(Guid MessageId, string ExternalMessageId);

public sealed record PagedWhatsAppConversations(
    PagedResponse<WhatsAppConversationResponse> Page);

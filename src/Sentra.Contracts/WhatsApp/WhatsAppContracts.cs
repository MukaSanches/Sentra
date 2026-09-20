namespace Sentra.Contracts.WhatsApp;

public sealed record WhatsAppIntegrationStatusResponse(
    string State,
    string? PhoneNumberId,
    string? DisplayName,
    DateTimeOffset? LastVerifiedAt,
    string? LastErrorCode);

public sealed record WhatsAppIntegrationVerifyResponse(
    string State,
    string PhoneNumberId,
    string VerifiedName,
    string DisplayPhoneNumber,
    string QualityRating,
    int ApprovedTemplateCount,
    int PublishedFlowCount,
    DateTimeOffset VerifiedAt);

public sealed record WhatsAppTemplateResponse(
    string Id,
    string Name,
    string Language,
    string Status,
    string Category);

public sealed record WhatsAppFlowResponse(
    string Id,
    string Name,
    string Status);

public sealed record ConversationSummaryResponse(
    Guid Id,
    string Channel,
    string ExternalParticipantId,
    string? DisplayName,
    Guid? ResidentId,
    Guid? UnitId,
    string Status,
    DateTimeOffset LastMessageAt);

public sealed record ConversationMessageResponse(
    Guid Id,
    string Direction,
    string ContentKind,
    string? Text,
    string? ExternalMessageId,
    Guid? ClientRequestId,
    DateTimeOffset OccurredAt,
    string DeliveryStatus,
    DateTimeOffset DeliveryStatusAt,
    string? LastErrorCode);

public sealed record SendWhatsAppTextRequest(
    Guid ClientRequestId,
    string Text);

public sealed record SendWhatsAppTemplateRequest(
    Guid ClientRequestId,
    string TemplateName,
    string LanguageCode,
    IReadOnlyList<string> BodyParameters);

public sealed record WhatsAppReplyButtonRequest(
    string Id,
    string Title);

public sealed record SendWhatsAppReplyButtonsRequest(
    Guid ClientRequestId,
    string Body,
    IReadOnlyList<WhatsAppReplyButtonRequest> Buttons);

public sealed record WhatsAppListRowRequest(
    string Id,
    string Title,
    string? Description);

public sealed record WhatsAppListSectionRequest(
    string? Title,
    IReadOnlyList<WhatsAppListRowRequest> Rows);

public sealed record SendWhatsAppListRequest(
    Guid ClientRequestId,
    string Body,
    string ButtonText,
    IReadOnlyList<WhatsAppListSectionRequest> Sections);

public sealed record SendWhatsAppFlowRequest(
    Guid ClientRequestId,
    string FlowId,
    string CallToAction,
    string Body,
    string? Screen,
    IReadOnlyDictionary<string, string>? Data);


public sealed record ConversationAttachmentResponse(
    Guid Id,
    Guid MessageId,
    string? MimeType,
    string? FileName,
    string StorageStatus);

public sealed record SendWhatsAppMediaResponse(
    string MediaId,
    ConversationMessageResponse Message);

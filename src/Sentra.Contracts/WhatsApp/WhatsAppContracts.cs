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
    DateTimeOffset VerifiedAt);

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

namespace Sentra.WhatsApp.Meta;

public sealed record MetaPhoneNumber(
    string Id,
    string? DisplayPhoneNumber,
    string? VerifiedName,
    string? QualityRating);

public sealed record MetaTemplate(string Id, string Name, string Language, string Status, string Category);

public sealed record MetaFlow(string Id, string Name, string Status);

public sealed record MetaMediaMetadata(
    string Id,
    string Url,
    string MimeType,
    string? Sha256,
    long? FileSize);

public sealed record MetaSendResult(string MessageId);

public sealed record MetaSubscription(string AppId, string? Name);

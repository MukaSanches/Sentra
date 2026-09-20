using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Abstractions;
using Sentra.Application.Realtime;
using Sentra.Domain.WhatsApp;
using Sentra.Infrastructure.Persistence;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp.Services;

public sealed class WhatsAppWebhookProcessor(
    SentraDbContext dbContext,
    IMetaWhatsAppClient meta,
    IRealtimeOperationsNotifier notifier,
    IClock clock) : IWhatsAppWebhookProcessor
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var webhookEvent = await dbContext.WhatsAppWebhookEvents
            .Where(x => x.ProcessingStatus == WebhookProcessingStatus.Pending)
            .Where(x => !x.NextAttemptAt.HasValue || x.NextAttemptAt <= now)
            .OrderBy(x => x.ReceivedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (webhookEvent is null) return false;

        webhookEvent.MarkProcessing(now);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await ProcessPayloadAsync(webhookEvent.PayloadJson, cancellationToken);
            webhookEvent.MarkProcessed(clock.UtcNow);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            webhookEvent.ScheduleRetry(SafeError(exception), clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ProcessPayloadAsync(string json, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("entry", out var entries)
            || entries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes)
                || changes.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;
                var phoneNumberId = GetNestedString(value, "metadata", "phone_number_id");
                if (string.IsNullOrWhiteSpace(phoneNumberId)) continue;

                var integration = await dbContext.WhatsAppIntegrations
                    .SingleOrDefaultAsync(
                        x => x.PhoneNumberId == phoneNumberId && x.IsEnabled,
                        cancellationToken);
                if (integration is null) continue;

                var contactNames = ContactNames(value);

                if (value.TryGetProperty("messages", out var messages)
                    && messages.ValueKind == JsonValueKind.Array)
                {
                    foreach (var message in messages.EnumerateArray())
                    {
                        await ProcessInboundAsync(
                            integration,
                            message,
                            contactNames,
                            cancellationToken);
                    }
                }

                if (value.TryGetProperty("statuses", out var statuses)
                    && statuses.ValueKind == JsonValueKind.Array)
                {
                    foreach (var status in statuses.EnumerateArray())
                    {
                        await ProcessStatusAsync(
                            integration.CondominiumId,
                            status,
                            cancellationToken);
                    }
                }

                integration.RecordWebhook(clock.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task ProcessInboundAsync(
        WhatsAppIntegration integration,
        JsonElement source,
        IReadOnlyDictionary<string, string> contactNames,
        CancellationToken cancellationToken)
    {
        var externalMessageId = GetString(source, "id");
        var participant = GetString(source, "from");
        if (string.IsNullOrWhiteSpace(externalMessageId)
            || string.IsNullOrWhiteSpace(participant))
        {
            return;
        }

        var timestamp = ParseTimestamp(GetString(source, "timestamp")) ?? clock.UtcNow;
        var displayName = contactNames.TryGetValue(participant, out var knownName)
            ? knownName
            : null;

        var residentId = await ResolveResidentAsync(
            integration.CondominiumId,
            participant,
            cancellationToken);

        var conversation = await dbContext.WhatsAppConversations
            .SingleOrDefaultAsync(
                x => x.CondominiumId == integration.CondominiumId
                    && x.ExternalParticipantId == participant,
                cancellationToken);

        if (conversation is null)
        {
            conversation = new WhatsAppConversation(
                integration.CondominiumId,
                participant,
                "whatsapp-webhook",
                timestamp,
                residentId,
                displayName);
            dbContext.WhatsAppConversations.Add(conversation);
        }

        conversation.RecordInbound(timestamp, residentId, displayName);

        var existing = await dbContext.WhatsAppMessages
            .SingleOrDefaultAsync(x => x.ExternalMessageId == externalMessageId, cancellationToken);

        var parsed = ParseMessage(source);
        var message = existing;
        var created = false;

        if (message is null)
        {
            message = new WhatsAppMessage(
                integration.CondominiumId,
                conversation.Id,
                WhatsAppMessageDirection.Inbound,
                parsed.Type,
                timestamp,
                externalMessageId,
                parsed.Text,
                parsed.ReplyToMessageId,
                source.GetRawText());

            dbContext.WhatsAppMessages.Add(message);
            created = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(parsed.MediaId))
        {
            await EnsureAttachmentAsync(
                integration.CondominiumId,
                message,
                parsed,
                cancellationToken);
        }

        await meta.MarkReadAsync(externalMessageId, cancellationToken);

        if (created)
        {
            await notifier.WhatsAppMessageReceivedAsync(
                integration.CondominiumId,
                conversation.Id,
                message.Id,
                cancellationToken);
        }
    }

    private async Task EnsureAttachmentAsync(
        Guid condominiumId,
        WhatsAppMessage message,
        ParsedInbound parsed,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.WhatsAppAttachments.AsNoTracking()
            .AnyAsync(
                x => x.MessageId == message.Id && x.MediaId == parsed.MediaId,
                cancellationToken);
        if (exists) return;

        var metadata = await meta.GetMediaMetadataAsync(parsed.MediaId!, cancellationToken);
        var content = await meta.DownloadMediaAsync(metadata.Url, cancellationToken);

        if (!HashMatches(content, metadata.Sha256 ?? parsed.Sha256))
            throw new InvalidDataException("Downloaded WhatsApp media hash does not match Meta metadata.");

        var attachment = new WhatsAppAttachment(
            condominiumId,
            message.Id,
            metadata.Id,
            metadata.MimeType,
            clock.UtcNow,
            parsed.FileName,
            metadata.Sha256 ?? parsed.Sha256,
            metadata.FileSize);

        attachment.Store(content, clock.UtcNow);
        dbContext.WhatsAppAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessStatusAsync(
        Guid condominiumId,
        JsonElement source,
        CancellationToken cancellationToken)
    {
        var externalId = GetString(source, "id");
        var rawStatus = GetString(source, "status");
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(rawStatus))
            return;

        var message = await dbContext.WhatsAppMessages
            .SingleOrDefaultAsync(
                x => x.CondominiumId == condominiumId && x.ExternalMessageId == externalId,
                cancellationToken);
        if (message is null) return;

        var status = rawStatus.ToLowerInvariant() switch
        {
            "sent" => WhatsAppDeliveryStatus.Sent,
            "delivered" => WhatsAppDeliveryStatus.Delivered,
            "read" => WhatsAppDeliveryStatus.Read,
            "failed" => WhatsAppDeliveryStatus.Failed,
            "deleted" => WhatsAppDeliveryStatus.Deleted,
            _ => (WhatsAppDeliveryStatus?)null
        };
        if (!status.HasValue) return;

        var timestamp = ParseTimestamp(GetString(source, "timestamp")) ?? clock.UtcNow;
        string? errorCode = null;
        string? errorTitle = null;
        if (source.TryGetProperty("errors", out var errors)
            && errors.ValueKind == JsonValueKind.Array)
        {
            var error = errors.EnumerateArray().FirstOrDefault();
            if (error.ValueKind == JsonValueKind.Object)
            {
                errorCode = error.TryGetProperty("code", out var code) ? code.ToString() : null;
                errorTitle = GetString(error, "title") ?? GetString(error, "message");
            }
        }

        if (message.ApplyDeliveryStatus(status.Value, timestamp, errorCode, errorTitle))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await notifier.WhatsAppMessageStatusChangedAsync(
                condominiumId,
                message.ConversationId,
                message.Id,
                cancellationToken);
        }
    }

    private async Task<Guid?> ResolveResidentAsync(
        Guid condominiumId,
        string participant,
        CancellationToken cancellationToken)
    {
        var e164 = "+" + participant;
        var ids = await (
            from phone in dbContext.ResidentPhones.AsNoTracking()
            join resident in dbContext.Residents.AsNoTracking()
                on phone.ResidentId equals resident.Id
            where phone.CondominiumId == condominiumId
                && phone.E164Number == e164
                && phone.IsWhatsAppEnabled
                && resident.IsActive
            select resident.Id)
            .Distinct()
            .Take(2)
            .ToListAsync(cancellationToken);

        return ids.Count == 1 ? ids[0] : null;
    }

    private static ParsedInbound ParseMessage(JsonElement source)
    {
        var rawType = GetString(source, "type")?.ToLowerInvariant() ?? "unknown";
        var replyTo = GetNestedString(source, "context", "id");

        return rawType switch
        {
            "text" => new(WhatsAppMessageType.Text, GetNestedString(source, "text", "body"), replyTo),
            "image" => Media(source, "image", WhatsAppMessageType.Image, replyTo),
            "audio" => Media(source, "audio", WhatsAppMessageType.Audio, replyTo),
            "document" => Media(source, "document", WhatsAppMessageType.Document, replyTo),
            "video" => Media(source, "video", WhatsAppMessageType.Video, replyTo),
            "sticker" => Media(source, "sticker", WhatsAppMessageType.Sticker, replyTo),
            "button" => new(
                WhatsAppMessageType.Button,
                GetNestedString(source, "button", "text") ?? GetNestedString(source, "button", "payload"),
                replyTo),
            "interactive" => Interactive(source, replyTo),
            "reaction" => new(
                WhatsAppMessageType.Reaction,
                GetNestedString(source, "reaction", "emoji"),
                GetNestedString(source, "reaction", "message_id")),
            "location" => new(
                WhatsAppMessageType.Location,
                source.TryGetProperty("location", out var location) ? location.GetRawText() : null,
                replyTo),
            "contacts" => new(
                WhatsAppMessageType.Contacts,
                source.TryGetProperty("contacts", out var contacts) ? contacts.GetRawText() : null,
                replyTo),
            _ => new(WhatsAppMessageType.Unknown, null, replyTo)
        };
    }

    private static ParsedInbound Interactive(JsonElement source, string? replyTo)
    {
        if (!source.TryGetProperty("interactive", out var interactive))
            return new(WhatsAppMessageType.Interactive, null, replyTo);

        if (interactive.TryGetProperty("button_reply", out var button))
            return new(
                WhatsAppMessageType.Interactive,
                GetString(button, "title") ?? GetString(button, "id"),
                replyTo);

        if (interactive.TryGetProperty("list_reply", out var list))
            return new(
                WhatsAppMessageType.Interactive,
                GetString(list, "title") ?? GetString(list, "id"),
                replyTo);

        if (interactive.TryGetProperty("nfm_reply", out var flow))
            return new(
                WhatsAppMessageType.Flow,
                GetString(flow, "response_json") ?? flow.GetRawText(),
                replyTo);

        return new(WhatsAppMessageType.Interactive, interactive.GetRawText(), replyTo);
    }

    private static ParsedInbound Media(
        JsonElement source,
        string property,
        WhatsAppMessageType type,
        string? replyTo)
    {
        if (!source.TryGetProperty(property, out var media))
            return new(type, null, replyTo);

        return new(
            type,
            GetString(media, "caption"),
            replyTo,
            GetString(media, "id"),
            GetString(media, "mime_type"),
            GetString(media, "sha256"),
            GetString(media, "filename"));
    }

    private static Dictionary<string, string> ContactNames(JsonElement value)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!value.TryGetProperty("contacts", out var contacts)
            || contacts.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var contact in contacts.EnumerateArray())
        {
            var id = GetString(contact, "wa_id");
            var name = GetNestedString(contact, "profile", "name");
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                result[id] = name;
        }
        return result;
    }

    private static DateTimeOffset? ParseTimestamp(string? value)
        => long.TryParse(value, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;

    private static string? GetNestedString(JsonElement element, string parent, string child)
        => element.TryGetProperty(parent, out var nested) ? GetString(nested, child) : null;

    private static string? GetString(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    private static bool HashMatches(byte[] content, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected)) return true;
        var hash = SHA256.HashData(content);
        var hex = Convert.ToHexString(hash);
        var base64 = Convert.ToBase64String(hash);
        return string.Equals(hex, expected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(base64, expected, StringComparison.Ordinal);
    }

    private static string SafeError(Exception exception)
        => exception switch
        {
            MetaWhatsAppException metaError => $"MetaWhatsAppException HTTP {metaError.StatusCode}",
            JsonException => "Invalid webhook JSON structure",
            InvalidDataException => "Media integrity validation failed",
            _ => exception.GetType().Name
        };

    private sealed record ParsedInbound(
        WhatsAppMessageType Type,
        string? Text,
        string? ReplyToMessageId,
        string? MediaId = null,
        string? MimeType = null,
        string? Sha256 = null,
        string? FileName = null);
}

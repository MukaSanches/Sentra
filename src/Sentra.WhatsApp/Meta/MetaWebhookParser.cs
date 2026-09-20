using System.Globalization;
using System.Text.Json;
using Sentra.Application.Integrations.WhatsApp;

namespace Sentra.WhatsApp.Meta;

public sealed class MetaWebhookParser : IWhatsAppWebhookParser
{
    public WhatsAppWebhookEnvelope Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Payload do webhook está vazio.", nameof(json));
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("object", out var objectName) ||
            !string.Equals(
                objectName.GetString(),
                "whatsapp_business_account",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Payload não pertence a whatsapp_business_account.");
        }

        var changes = new List<WhatsAppWebhookChange>();

        if (!root.TryGetProperty("entry", out var entries) ||
            entries.ValueKind != JsonValueKind.Array)
        {
            return new WhatsAppWebhookEnvelope(changes);
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var entryChanges) ||
                entryChanges.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var change in entryChanges.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                {
                    continue;
                }

                var phoneNumberId = GetNestedString(
                    value,
                    "metadata",
                    "phone_number_id");

                if (string.IsNullOrWhiteSpace(phoneNumberId))
                {
                    continue;
                }

                var (contactName, contactWaId) = ReadContact(value);
                var messages = ReadMessages(value);
                var statuses = ReadStatuses(value);

                changes.Add(
                    new WhatsAppWebhookChange(
                        phoneNumberId,
                        contactName,
                        contactWaId,
                        messages,
                        statuses));
            }
        }

        return new WhatsAppWebhookEnvelope(changes);
    }

    private static (string? Name, string? WaId) ReadContact(JsonElement value)
    {
        if (!value.TryGetProperty("contacts", out var contacts) ||
            contacts.ValueKind != JsonValueKind.Array ||
            contacts.GetArrayLength() == 0)
        {
            return (null, null);
        }

        var contact = contacts.EnumerateArray().First();
        var waId = GetString(contact, "wa_id");
        var name = GetNestedString(contact, "profile", "name");
        return (name, waId);
    }

    private static IReadOnlyList<WhatsAppIncomingMessage> ReadMessages(
        JsonElement value)
    {
        var result = new List<WhatsAppIncomingMessage>();

        if (!value.TryGetProperty("messages", out var messages) ||
            messages.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var message in messages.EnumerateArray())
        {
            var from = GetString(message, "from");
            var id = GetString(message, "id");
            var timestamp = ParseTimestamp(GetString(message, "timestamp"));
            var type = GetString(message, "type");

            if (string.IsNullOrWhiteSpace(from) ||
                string.IsNullOrWhiteSpace(id) ||
                timestamp is null)
            {
                continue;
            }

            var kind = type switch
            {
                "text" => WhatsAppIncomingKind.Text,
                "image" => WhatsAppIncomingKind.Image,
                "audio" => WhatsAppIncomingKind.Audio,
                "video" => WhatsAppIncomingKind.Video,
                "document" => WhatsAppIncomingKind.Document,
                "location" => WhatsAppIncomingKind.Location,
                "contacts" => WhatsAppIncomingKind.Contacts,
                "interactive" or "button" => WhatsAppIncomingKind.Interactive,
                "reaction" => WhatsAppIncomingKind.Reaction,
                _ => WhatsAppIncomingKind.Unknown
            };

            string? text = null;
            string? mediaId = null;
            string? mimeType = null;
            string? fileName = null;
            string? sha256 = null;
            string? structuredDataJson = null;

            if (kind == WhatsAppIncomingKind.Text &&
                message.TryGetProperty("text", out var textElement))
            {
                text = GetString(textElement, "body");
            }
            else if (kind is WhatsAppIncomingKind.Image
                     or WhatsAppIncomingKind.Audio
                     or WhatsAppIncomingKind.Video
                     or WhatsAppIncomingKind.Document)
            {
                var propertyName = kind switch
                {
                    WhatsAppIncomingKind.Image => "image",
                    WhatsAppIncomingKind.Audio => "audio",
                    WhatsAppIncomingKind.Video => "video",
                    _ => "document"
                };

                if (message.TryGetProperty(propertyName, out var media))
                {
                    mediaId = GetString(media, "id");
                    mimeType = GetString(media, "mime_type");
                    fileName = GetString(media, "filename");
                    sha256 = GetString(media, "sha256");
                    text = GetString(media, "caption");
                }
            }
            else if (kind == WhatsAppIncomingKind.Location &&
                     message.TryGetProperty("location", out var location))
            {
                structuredDataJson = location.GetRawText();
            }
            else if (kind == WhatsAppIncomingKind.Contacts &&
                     message.TryGetProperty("contacts", out var contacts))
            {
                structuredDataJson = contacts.GetRawText();
            }
            else if (kind == WhatsAppIncomingKind.Interactive)
            {
                if (message.TryGetProperty("interactive", out var interactive))
                {
                    structuredDataJson = interactive.GetRawText();
                }
                else if (message.TryGetProperty("button", out var button))
                {
                    structuredDataJson = button.GetRawText();
                }
            }
            else if (kind == WhatsAppIncomingKind.Reaction &&
                     message.TryGetProperty("reaction", out var reaction))
            {
                structuredDataJson = reaction.GetRawText();
            }

            result.Add(
                new WhatsAppIncomingMessage(
                    from,
                    id,
                    timestamp.Value,
                    kind,
                    text,
                    mediaId,
                    mimeType,
                    fileName,
                    sha256,
                    structuredDataJson));
        }

        return result;
    }

    private static IReadOnlyList<WhatsAppStatusUpdate> ReadStatuses(
        JsonElement value)
    {
        var result = new List<WhatsAppStatusUpdate>();

        if (!value.TryGetProperty("statuses", out var statuses) ||
            statuses.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var status in statuses.EnumerateArray())
        {
            var id = GetString(status, "id");
            var statusName = GetString(status, "status");
            var timestamp = ParseTimestamp(GetString(status, "timestamp"));

            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(statusName) ||
                timestamp is null)
            {
                continue;
            }

            string? errorCode = null;
            string? errorTitle = null;

            if (status.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array &&
                errors.GetArrayLength() > 0)
            {
                var error = errors[0];

                if (error.TryGetProperty("code", out var code))
                {
                    errorCode = code.ToString();
                }

                errorTitle = GetString(error, "title");
            }

            result.Add(
                new WhatsAppStatusUpdate(
                    id,
                    statusName,
                    timestamp.Value,
                    GetString(status, "recipient_id"),
                    errorCode,
                    errorTitle));
        }

        return result;
    }

    private static DateTimeOffset? ParseTimestamp(string? value)
    {
        if (!long.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var unixSeconds))
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) &&
           property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? GetNestedString(
        JsonElement element,
        string parent,
        string child)
        => element.TryGetProperty(parent, out var parentElement)
            ? GetString(parentElement, child)
            : null;
}

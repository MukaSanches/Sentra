using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Sentra.Contracts.WhatsApp;
using Sentra.WhatsApp.Configuration;

namespace Sentra.WhatsApp.Meta;

public sealed class MetaWhatsAppClient(
    IHttpClientFactory httpClientFactory,
    WhatsAppOptions options) : IMetaWhatsAppClient
{
    private const string ClientName = "MetaWhatsApp";

    public async Task<IReadOnlyList<MetaPhoneNumber>> GetPhoneNumbersAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{options.WabaId}/phone_numbers", null, cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        return document.RootElement.GetProperty("data").EnumerateArray()
            .Select(x => new MetaPhoneNumber(
                RequiredString(x, "id"),
                OptionalString(x, "display_phone_number"),
                OptionalString(x, "verified_name"),
                OptionalString(x, "quality_rating")))
            .ToArray();
    }

    public async Task<IReadOnlyList<MetaSubscription>> GetSubscriptionsAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{options.WabaId}/subscribed_apps", null, cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        var results = new List<MetaSubscription>();
        foreach (var item in document.RootElement.GetProperty("data").EnumerateArray())
        {
            if (!item.TryGetProperty("whatsapp_business_api_data", out var app)) continue;
            var id = OptionalString(app, "id");
            if (!string.IsNullOrWhiteSpace(id))
                results.Add(new MetaSubscription(id, OptionalString(app, "name")));
        }
        return results;
    }

    public async Task<bool> SubscribeAppAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Post, $"{options.WabaId}/subscribed_apps", new { }, cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        return document.RootElement.TryGetProperty("success", out var value)
            && (value.ValueKind == JsonValueKind.True
                || (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed) && parsed));
    }

    public async Task<IReadOnlyList<MetaTemplate>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{options.WabaId}/message_templates?limit=100", null, cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        return document.RootElement.GetProperty("data").EnumerateArray()
            .Select(x => new MetaTemplate(
                RequiredString(x, "id"),
                RequiredString(x, "name"),
                OptionalString(x, "language") ?? string.Empty,
                OptionalString(x, "status") ?? string.Empty,
                OptionalString(x, "category") ?? string.Empty))
            .ToArray();
    }

    public async Task<IReadOnlyList<MetaFlow>> GetFlowsAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{options.WabaId}/flows?fields=id,name,status&limit=100", null, cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        return document.RootElement.GetProperty("data").EnumerateArray()
            .Select(x => new MetaFlow(
                RequiredString(x, "id"),
                OptionalString(x, "name") ?? string.Empty,
                OptionalString(x, "status") ?? string.Empty))
            .ToArray();
    }

    public Task<MetaSendResult> SendTextAsync(
        string to,
        string text,
        string? replyToMessageId,
        CancellationToken cancellationToken)
    {
        object payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to,
            type = "text",
            context = string.IsNullOrWhiteSpace(replyToMessageId) ? null : new { message_id = replyToMessageId },
            text = new { preview_url = false, body = text }
        };
        return SendMessageAsync(payload, cancellationToken);
    }

    public Task<MetaSendResult> SendButtonsAsync(
        string to,
        string body,
        IReadOnlyList<WhatsAppReplyButton> buttons,
        CancellationToken cancellationToken)
    {
        if (buttons.Count is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(buttons), "WhatsApp reply buttons require 1 to 3 buttons.");

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new { text = body },
                action = new
                {
                    buttons = buttons.Select(x => new
                    {
                        type = "reply",
                        reply = new { id = x.Id, title = x.Title }
                    }).ToArray()
                }
            }
        };
        return SendMessageAsync(payload, cancellationToken);
    }

    public Task<MetaSendResult> SendListAsync(
        string to,
        string body,
        string buttonText,
        IReadOnlyList<WhatsAppListSection> sections,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to,
            type = "interactive",
            interactive = new
            {
                type = "list",
                body = new { text = body },
                action = new
                {
                    button = buttonText,
                    sections = sections.Select(section => new
                    {
                        title = section.Title,
                        rows = section.Rows.Select(row => new
                        {
                            id = row.Id,
                            title = row.Title,
                            description = row.Description
                        }).ToArray()
                    }).ToArray()
                }
            }
        };
        return SendMessageAsync(payload, cancellationToken);
    }

    public Task<MetaSendResult> SendTemplateAsync(
        string to,
        string templateName,
        string languageCode,
        IReadOnlyList<string>? bodyParameters,
        CancellationToken cancellationToken)
    {
        var components = bodyParameters is { Count: > 0 }
            ? new object[]
            {
                new
                {
                    type = "body",
                    parameters = bodyParameters.Select(value => new { type = "text", text = value }).ToArray()
                }
            }
            : Array.Empty<object>();

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components
            }
        };
        return SendMessageAsync(payload, cancellationToken);
    }

    public Task<MetaSendResult> SendMediaAsync(
        string to,
        string mediaId,
        string mediaType,
        string? caption,
        string? fileName,
        CancellationToken cancellationToken)
    {
        var normalizedType = mediaType.Trim().ToLowerInvariant();
        if (normalizedType is not ("image" or "audio" or "document" or "video" or "sticker"))
            throw new ArgumentException("Unsupported WhatsApp media type.", nameof(mediaType));

        var media = new Dictionary<string, object?> { ["id"] = mediaId };
        if (!string.IsNullOrWhiteSpace(caption) && normalizedType is "image" or "document" or "video")
            media["caption"] = caption;
        if (!string.IsNullOrWhiteSpace(fileName) && normalizedType == "document")
            media["filename"] = fileName;

        var payload = new Dictionary<string, object?>
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = to,
            ["type"] = normalizedType,
            [normalizedType] = media
        };
        return SendMessageAsync(payload, cancellationToken);
    }

    public Task<MetaSendResult> SendFlowAsync(
        SendWhatsAppFlowRequest request,
        CancellationToken cancellationToken)
    {
        JsonElement? actionData = null;
        if (!string.IsNullOrWhiteSpace(request.ActionDataJson))
        {
            using var actionDocument = JsonDocument.Parse(request.ActionDataJson);
            actionData = actionDocument.RootElement.Clone();
        }

        var parameters = new Dictionary<string, object?>
        {
            ["flow_message_version"] = "3",
            ["flow_token"] = request.FlowToken,
            ["flow_id"] = request.FlowId,
            ["flow_cta"] = request.ButtonText,
            ["flow_action"] = request.FlowAction
        };
        if (!string.IsNullOrWhiteSpace(request.Screen))
            parameters["flow_action_payload"] = new Dictionary<string, object?>
            {
                ["screen"] = request.Screen,
                ["data"] = actionData
            };

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = request.To,
            type = "interactive",
            interactive = new
            {
                type = "flow",
                body = new { text = request.Body },
                action = new
                {
                    name = "flow",
                    parameters
                }
            }
        };
        return SendMessageAsync(payload, cancellationToken);
    }

    public async Task<bool> MarkReadAsync(string messageId, CancellationToken cancellationToken)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            status = "read",
            message_id = messageId
        };
        using var response = await SendAsync(HttpMethod.Put, $"{options.PhoneNumberId}/messages", payload, cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        return document.RootElement.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True;
    }

    public async Task<MetaMediaMetadata> GetMediaMetadataAsync(string mediaId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"{mediaId}?phone_number_id={Uri.EscapeDataString(options.PhoneNumberId)}",
            null,
            cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        var root = document.RootElement;
        return new(
            RequiredString(root, "id"),
            RequiredString(root, "url"),
            RequiredString(root, "mime_type"),
            OptionalString(root, "sha256"),
            root.TryGetProperty("file_size", out var size) && size.TryGetInt64(out var value) ? value : null);
    }

    public async Task<byte[]> DownloadMediaAsync(string absoluteUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Media URL must be absolute HTTPS.", nameof(absoluteUrl));

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", RequiredToken());
        using var response = await httpClientFactory.CreateClient(ClientName)
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<string> UploadMediaAsync(
        Stream stream,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("whatsapp"), "messaging_product");
        var mediaContent = new StreamContent(stream);
        mediaContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
        content.Add(mediaContent, "file", fileName);

        using var request = CreateRequest(HttpMethod.Post, $"{options.PhoneNumberId}/media");
        request.Content = content;
        using var response = await httpClientFactory.CreateClient(ClientName)
            .SendAsync(request, cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        return RequiredString(document.RootElement, "id");
    }

    private async Task<MetaSendResult> SendMessageAsync(object payload, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Post, $"{options.PhoneNumberId}/messages", payload, cancellationToken);
        using var document = await ParseSuccessAsync(response, cancellationToken);
        var messages = document.RootElement.GetProperty("messages");
        var first = messages.EnumerateArray().FirstOrDefault();
        return new MetaSendResult(RequiredString(first, "id"));
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        object? payload,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, relativePath);
        if (payload is not null) request.Content = JsonContent.Create(payload);
        return await httpClientFactory.CreateClient(ClientName)
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var version = options.GraphApiVersion.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("META_GRAPH_API_VERSION is not configured.");

        var uri = new Uri($"https://graph.facebook.com/{version}/{relativePath.TrimStart('/')}");
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", RequiredToken());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private string RequiredToken()
        => string.IsNullOrWhiteSpace(options.AccessToken)
            ? throw new InvalidOperationException("META_ACCESS_TOKEN is not configured.")
            : options.AccessToken;

    private static async Task<JsonDocument> ParseSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new MetaWhatsAppException((int)response.StatusCode, body);
    }

    private static string RequiredString(JsonElement element, string property)
        => OptionalString(element, property)
            ?? throw new InvalidOperationException($"Meta response is missing '{property}'.");

    private static string? OptionalString(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
}

public sealed class MetaWhatsAppException(int statusCode, string responseBody)
    : Exception($"Meta WhatsApp API returned HTTP {statusCode}.")
{
    public int StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}

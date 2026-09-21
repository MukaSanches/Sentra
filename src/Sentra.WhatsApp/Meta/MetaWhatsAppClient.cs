using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Sentra.Application.Integrations.WhatsApp;
using Sentra.Domain.Residents;

namespace Sentra.WhatsApp.Meta;

public sealed class MetaWhatsAppClient(
    HttpClient httpClient,
    IConfiguration configuration)
    : IWhatsAppClient
{
    public async Task<WhatsAppPhoneInfo> GetPhoneInfoAsync(
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        using var request = CreateRequest(
            HttpMethod.Get,
            settings,
            $"{settings.GraphVersion}/{settings.PhoneNumberId}?fields=id,verified_name,display_phone_number,quality_rating");

        using var response =
            await httpClient.SendAsync(request, cancellationToken);

        await response.EnsureMetaSuccessAsync(cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<PhoneInfoPayload>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidDataException(
                "A Meta retornou uma resposta vazia para o número.");

        return MapPhoneInfo(payload, settings.PhoneNumberId);
    }

    public async Task<IReadOnlyList<WhatsAppPhoneInfo>> GetWabaPhoneNumbersAsync(
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        var items = await GetAllPagesAsync<PhoneInfoPayload>(
            settings,
            $"{settings.GraphVersion}/{settings.WabaId}/phone_numbers?fields=id,verified_name,display_phone_number,quality_rating&limit=100",
            cancellationToken);

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Select(item => MapPhoneInfo(item, item.Id!))
            .ToArray();
    }

    public async Task SubscribeWabaAsync(CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        using var request = CreateRequest(
            HttpMethod.Post,
            settings,
            $"{settings.GraphVersion}/{settings.WabaId}/subscribed_apps");

        using var response =
            await httpClient.SendAsync(request, cancellationToken);

        await response.EnsureMetaSuccessAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WhatsAppTemplateInfo>> GetTemplatesAsync(
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        var items = await GetAllPagesAsync<TemplatePayload>(
            settings,
            $"{settings.GraphVersion}/{settings.WabaId}/message_templates?fields=id,name,language,status,category&limit=100",
            cancellationToken);

        return items
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Id) &&
                !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new WhatsAppTemplateInfo(
                item.Id!,
                item.Name!,
                item.Language ?? string.Empty,
                item.Status ?? "UNKNOWN",
                item.Category ?? "UNKNOWN"))
            .ToArray();
    }

    public async Task<IReadOnlyList<WhatsAppFlowInfo>> GetFlowsAsync(
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        var items = await GetAllPagesAsync<FlowPayload>(
            settings,
            $"{settings.GraphVersion}/{settings.WabaId}/flows?fields=id,name,status&limit=100",
            cancellationToken);

        return items
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Id) &&
                !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new WhatsAppFlowInfo(
                item.Id!,
                item.Name!,
                item.Status ?? "UNKNOWN"))
            .ToArray();
    }

    public Task<WhatsAppSendResult> SendTextAsync(
        string recipientE164,
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Mensagem não pode ser vazia.", nameof(text));
        }

        if (text.Length > 4096)
        {
            throw new ArgumentOutOfRangeException(
                nameof(text),
                "Mensagem excede 4096 caracteres.");
        }

        return SendMessageAsync(
            recipientE164,
            new
            {
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = text
                }
            },
            cancellationToken);
    }

    public Task<WhatsAppSendResult> SendTemplateAsync(
        string recipientE164,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken)
    {
        var normalizedName = RequireToken(templateName, nameof(templateName), 512);
        var normalizedLanguage = RequireToken(languageCode, nameof(languageCode), 32);

        var parameters = bodyParameters
            .Select((value, index) => new
            {
                type = "text",
                text = RequireText(value, $"bodyParameters[{index}]", 1024)
            })
            .ToArray();

        object[] components = parameters.Length == 0
            ? []
            : [new { type = "body", parameters }];

        return SendMessageAsync(
            recipientE164,
            new
            {
                type = "template",
                template = new
                {
                    name = normalizedName,
                    language = new { code = normalizedLanguage },
                    components
                }
            },
            cancellationToken);
    }

    public Task<WhatsAppSendResult> SendReplyButtonsAsync(
        string recipientE164,
        string body,
        IReadOnlyList<WhatsAppInteractiveButton> buttons,
        CancellationToken cancellationToken)
    {
        var normalizedBody = RequireText(body, nameof(body), 4096);

        if (buttons.Count is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(buttons),
                "Mensagens de botões devem conter entre 1 e 3 opções.");
        }

        var normalizedButtons = buttons
            .Select(button => new
            {
                type = "reply",
                reply = new
                {
                    id = RequireToken(button.Id, nameof(buttons), 256),
                    title = RequireText(button.Title, nameof(buttons), 20)
                }
            })
            .ToArray();

        return SendMessageAsync(
            recipientE164,
            new
            {
                type = "interactive",
                interactive = new
                {
                    type = "button",
                    body = new { text = normalizedBody },
                    action = new { buttons = normalizedButtons }
                }
            },
            cancellationToken);
    }

    public Task<WhatsAppSendResult> SendListAsync(
        string recipientE164,
        string body,
        string buttonText,
        IReadOnlyList<WhatsAppListSection> sections,
        CancellationToken cancellationToken)
    {
        var normalizedBody = RequireText(body, nameof(body), 4096);
        var normalizedButton = RequireText(buttonText, nameof(buttonText), 20);

        if (sections.Count == 0)
        {
            throw new ArgumentException(
                "A lista precisa de pelo menos uma seção.",
                nameof(sections));
        }

        var totalRows = sections.Sum(section => section.Rows.Count);
        if (totalRows is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sections),
                "A lista deve conter entre 1 e 10 opções.");
        }

        var normalizedSections = sections
            .Select(section => new
            {
                title = string.IsNullOrWhiteSpace(section.Title)
                    ? null
                    : RequireText(section.Title, nameof(sections), 24),
                rows = section.Rows.Select(row => new
                {
                    id = RequireToken(row.Id, nameof(sections), 200),
                    title = RequireText(row.Title, nameof(sections), 24),
                    description = string.IsNullOrWhiteSpace(row.Description)
                        ? null
                        : RequireText(row.Description, nameof(sections), 72)
                }).ToArray()
            })
            .ToArray();

        return SendMessageAsync(
            recipientE164,
            new
            {
                type = "interactive",
                interactive = new
                {
                    type = "list",
                    body = new { text = normalizedBody },
                    action = new
                    {
                        button = normalizedButton,
                        sections = normalizedSections
                    }
                }
            },
            cancellationToken);
    }

    public Task<WhatsAppSendResult> SendFlowAsync(
        string recipientE164,
        string flowId,
        string flowToken,
        string callToAction,
        string body,
        string? screen,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken)
    {
        var normalizedFlowId = RequireToken(flowId, nameof(flowId), 160);
        var normalizedFlowToken = RequireToken(flowToken, nameof(flowToken), 512);
        var normalizedCta = RequireText(callToAction, nameof(callToAction), 30);
        var normalizedBody = RequireText(body, nameof(body), 4096);

        Dictionary<string, string>? normalizedData = null;
        if (data is not null && data.Count > 0)
        {
            normalizedData = data.ToDictionary(
                pair => RequireToken(pair.Key, nameof(data), 128),
                pair => RequireText(pair.Value, nameof(data), 2048),
                StringComparer.Ordinal);
        }

        object? flowActionPayload = string.IsNullOrWhiteSpace(screen) &&
            (normalizedData is null || normalizedData.Count == 0)
                ? null
                : new
                {
                    screen = string.IsNullOrWhiteSpace(screen)
                        ? null
                        : RequireToken(screen, nameof(screen), 128),
                    data = normalizedData
                };

        return SendMessageAsync(
            recipientE164,
            new
            {
                type = "interactive",
                interactive = new
                {
                    type = "flow",
                    body = new { text = normalizedBody },
                    action = new
                    {
                        name = "flow",
                        parameters = new
                        {
                            flow_message_version = "3",
                            flow_action = "navigate",
                            flow_token = normalizedFlowToken,
                            flow_id = normalizedFlowId,
                            flow_cta = normalizedCta,
                            flow_action_payload = flowActionPayload
                        }
                    }
                }
            },
            cancellationToken);
    }

    public async Task MarkMessageReadAsync(
        string messageId,
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        var normalizedMessageId = RequireToken(messageId, nameof(messageId), 512);

        using var request = CreateRequest(
            HttpMethod.Post,
            settings,
            $"{settings.GraphVersion}/{settings.PhoneNumberId}/messages");

        request.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            status = "read",
            message_id = normalizedMessageId
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureMetaSuccessAsync(cancellationToken);
    }

    public async Task MarkMessageReadWithTypingAsync(
        string messageId,
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        var normalizedMessageId = RequireToken(messageId, nameof(messageId), 512);

        using var request = CreateRequest(
            HttpMethod.Post,
            settings,
            $"{settings.GraphVersion}/{settings.PhoneNumberId}/messages");

        request.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            status = "read",
            message_id = normalizedMessageId,
            typing_indicator = new
            {
                type = "text"
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureMetaSuccessAsync(cancellationToken);
    }

    public async Task<WhatsAppMediaInfo> GetMediaInfoAsync(
        string mediaId,
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        ValidateNumericId(mediaId, nameof(mediaId));

        using var request = CreateRequest(
            HttpMethod.Get,
            settings,
            $"{settings.GraphVersion}/{mediaId}?phone_number_id={settings.PhoneNumberId}");

        using var response =
            await httpClient.SendAsync(request, cancellationToken);

        await response.EnsureMetaSuccessAsync(cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<MediaInfoPayload>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidDataException(
                "A Meta retornou dados de mídia vazios.");

        if (!Uri.TryCreate(payload.Url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException(
                "A Meta retornou uma URL de mídia inválida.");
        }

        return new WhatsAppMediaInfo(
            payload.Id ?? mediaId,
            uri,
            payload.MimeType,
            payload.Sha256,
            payload.FileSize);
    }

    public async Task DownloadMediaAsync(
        string mediaId,
        Stream destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "O destino da mídia precisa aceitar escrita.",
                nameof(destination));
        }

        var settings = MetaWhatsAppConfiguration.From(configuration);
        var media = await GetMediaInfoAsync(mediaId, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, media.Url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.AccessToken);

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await response.EnsureMetaSuccessAsync(cancellationToken);

        await response.Content.CopyToAsync(destination, cancellationToken);
    }

    public async Task<WhatsAppMediaUploadResult> UploadMediaAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
        {
            throw new ArgumentException(
                "O conteúdo da mídia precisa aceitar leitura.",
                nameof(content));
        }

        var settings = MetaWhatsAppConfiguration.From(configuration);
        var normalizedFileName = RequireText(fileName, nameof(fileName), 255);
        var normalizedContentType = RequireToken(contentType, nameof(contentType), 160);

        using var request = CreateRequest(
            HttpMethod.Post,
            settings,
            $"{settings.GraphVersion}/{settings.PhoneNumberId}/media");

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("whatsapp"), "messaging_product");

        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(normalizedContentType);
        multipart.Add(streamContent, "file", normalizedFileName);
        request.Content = multipart;

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await response.EnsureMetaSuccessAsync(cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<MediaUploadPayload>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidDataException(
                "A Meta retornou uma resposta vazia ao enviar mídia.");

        if (string.IsNullOrWhiteSpace(payload.Id))
        {
            throw new InvalidDataException(
                "A Meta não retornou o Media ID.");
        }

        return new WhatsAppMediaUploadResult(payload.Id);
    }

    public Task<WhatsAppSendResult> SendMediaAsync(
        string recipientE164,
        WhatsAppMediaKind kind,
        string mediaId,
        string? caption,
        string? fileName,
        CancellationToken cancellationToken)
    {
        ValidateNumericId(mediaId, nameof(mediaId));

        var type = kind switch
        {
            WhatsAppMediaKind.Image => "image",
            WhatsAppMediaKind.Audio => "audio",
            WhatsAppMediaKind.Video => "video",
            WhatsAppMediaKind.Document => "document",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        var normalizedCaption = string.IsNullOrWhiteSpace(caption)
            ? null
            : RequireText(caption, nameof(caption), 1024);
        var normalizedFileName = string.IsNullOrWhiteSpace(fileName)
            ? null
            : RequireText(fileName, nameof(fileName), 255);

        object media = kind switch
        {
            WhatsAppMediaKind.Document => new
            {
                id = mediaId,
                caption = normalizedCaption,
                filename = normalizedFileName
            },
            WhatsAppMediaKind.Image or WhatsAppMediaKind.Video => new
            {
                id = mediaId,
                caption = normalizedCaption
            },
            _ => new { id = mediaId }
        };

        return SendMessageAsync(
            recipientE164,
            new Dictionary<string, object?>
            {
                ["type"] = type,
                [type] = media
            },
            cancellationToken);
    }

    private async Task<WhatsAppSendResult> SendMessageAsync(
        string recipientE164,
        object messagePayload,
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        var normalizedRecipient = ResidentPhone.NormalizeE164(recipientE164);

        using var request = CreateRequest(
            HttpMethod.Post,
            settings,
            $"{settings.GraphVersion}/{settings.PhoneNumberId}/messages");

        using var payloadDocument = JsonDocument.Parse(
            JsonSerializer.Serialize(messagePayload));
        var root = payloadDocument.RootElement;

        var envelope = new Dictionary<string, object?>
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = normalizedRecipient.TrimStart('+')
        };

        foreach (var property in root.EnumerateObject())
        {
            envelope[property.Name] = JsonSerializer.Deserialize<object>(
                property.Value.GetRawText());
        }

        request.Content = JsonContent.Create(envelope);

        using var response =
            await httpClient.SendAsync(request, cancellationToken);

        await response.EnsureMetaSuccessAsync(cancellationToken);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        if (!document.RootElement.TryGetProperty("messages", out var messages) ||
            messages.ValueKind != JsonValueKind.Array ||
            messages.GetArrayLength() == 0 ||
            !messages.EnumerateArray().First().TryGetProperty("id", out var idElement) ||
            string.IsNullOrWhiteSpace(idElement.GetString()))
        {
            throw new InvalidDataException(
                "A Meta não retornou o identificador da mensagem enviada.");
        }

        return new WhatsAppSendResult(idElement.GetString()!);
    }

    private async Task<IReadOnlyList<T>> GetAllPagesAsync<T>(
        MetaWhatsAppConfiguration settings,
        string initialRelativeUri,
        CancellationToken cancellationToken)
    {
        if (httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException(
                "Meta Graph API BaseAddress não está configurado.");
        }

        var items = new List<T>();
        Uri? next = new Uri(httpClient.BaseAddress, initialRelativeUri);
        var pageCount = 0;

        while (next is not null)
        {
            pageCount++;

            if (pageCount > 100)
            {
                throw new InvalidDataException(
                    "A paginação da Meta excedeu o limite de segurança.");
            }

            next = NormalizeGraphPagingUri(next);

            using var request = new HttpRequestMessage(HttpMethod.Get, next);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.AccessToken);

            using var response =
                await httpClient.SendAsync(request, cancellationToken);

            await response.EnsureMetaSuccessAsync(cancellationToken);

            var page = await response.Content.ReadFromJsonAsync<PagePayload<T>>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidDataException(
                    "A Meta retornou uma página vazia.");

            if (page.Data is not null)
            {
                items.AddRange(page.Data);
            }

            next = string.IsNullOrWhiteSpace(page.Paging?.Next)
                ? null
                : ParsePagingUri(page.Paging.Next);
        }

        return items;
    }

    private static Uri ParsePagingUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidDataException(
                "A Meta retornou uma URL de paginação inválida.");
        }

        return uri;
    }

    private static Uri NormalizeGraphPagingUri(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "graph.facebook.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "A paginação da Meta apontou para um host não permitido.");
        }

        var builder = new UriBuilder(uri);
        var parameters = builder.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(parameter =>
                !parameter.StartsWith(
                    "access_token=",
                    StringComparison.OrdinalIgnoreCase));

        builder.Query = string.Join("&", parameters);
        return builder.Uri;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        MetaWhatsAppConfiguration settings,
        string relativeUri)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        return request;
    }

    private static WhatsAppPhoneInfo MapPhoneInfo(
        PhoneInfoPayload payload,
        string fallbackId)
        => new(
            payload.Id ?? fallbackId,
            payload.VerifiedName ?? string.Empty,
            payload.DisplayPhoneNumber ?? string.Empty,
            payload.QualityRating ?? "UNKNOWN");

    private static string RequireText(
        string? value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "O valor é obrigatório.",
                parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : throw new ArgumentOutOfRangeException(parameterName);
    }

    private static string RequireToken(
        string? value,
        string parameterName,
        int maxLength)
    {
        var normalized = RequireText(value, parameterName, maxLength);
        if (normalized.Any(char.IsControl))
        {
            throw new ArgumentException(
                "O valor contém caracteres de controle.",
                parameterName);
        }

        return normalized;
    }

    private static void ValidateNumericId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Any(character => !char.IsDigit(character)))
        {
            throw new ArgumentException(
                "Identificador deve conter somente dígitos.",
                parameterName);
        }
    }

    private sealed record PhoneInfoPayload(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("verified_name")] string? VerifiedName,
        [property: JsonPropertyName("display_phone_number")] string? DisplayPhoneNumber,
        [property: JsonPropertyName("quality_rating")] string? QualityRating);

    private sealed record PagePayload<T>(
        [property: JsonPropertyName("data")] IReadOnlyList<T>? Data,
        [property: JsonPropertyName("paging")] PagingPayload? Paging);

    private sealed record PagingPayload(
        [property: JsonPropertyName("next")] string? Next);

    private sealed record PhoneListPayload(
        [property: JsonPropertyName("data")] IReadOnlyList<PhoneInfoPayload> Data);

    private sealed record TemplatePayload(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("category")] string? Category);

    private sealed record TemplateListPayload(
        [property: JsonPropertyName("data")] IReadOnlyList<TemplatePayload> Data);

    private sealed record FlowPayload(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("status")] string? Status);

    private sealed record FlowListPayload(
        [property: JsonPropertyName("data")] IReadOnlyList<FlowPayload> Data);

    private sealed record MediaInfoPayload(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("mime_type")] string? MimeType,
        [property: JsonPropertyName("sha256")] string? Sha256,
        [property: JsonPropertyName("file_size")] long? FileSize);

    private sealed record MediaUploadPayload(
        [property: JsonPropertyName("id")] string? Id);
}

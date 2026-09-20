using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
            $"{settings.GraphVersion}/{settings.PhoneNumberId}");

        using var response =
            await httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PhoneInfoPayload>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidDataException(
                "A Meta retornou uma resposta vazia para o número.");

        return new WhatsAppPhoneInfo(
            payload.Id ?? settings.PhoneNumberId,
            payload.VerifiedName ?? string.Empty,
            payload.DisplayPhoneNumber ?? string.Empty,
            payload.QualityRating ?? "UNKNOWN");
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

        response.EnsureSuccessStatusCode();
    }

    public async Task<WhatsAppSendResult> SendTextAsync(
        string recipientE164,
        string text,
        CancellationToken cancellationToken)
    {
        var settings = MetaWhatsAppConfiguration.From(configuration);
        var normalizedRecipient = ResidentPhone.NormalizeE164(recipientE164);

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

        using var request = CreateRequest(
            HttpMethod.Post,
            settings,
            $"{settings.GraphVersion}/{settings.PhoneNumberId}/messages");

        request.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = normalizedRecipient.TrimStart('+'),
            type = "text",
            text = new
            {
                preview_url = false,
                body = text
            }
        });

        using var response =
            await httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));

        if (!document.RootElement.TryGetProperty("messages", out var messages) ||
            messages.ValueKind != JsonValueKind.Array ||
            messages.GetArrayLength() == 0 ||
            !messages[0].TryGetProperty("id", out var idElement) ||
            string.IsNullOrWhiteSpace(idElement.GetString()))
        {
            throw new InvalidDataException(
                "A Meta não retornou o identificador da mensagem enviada.");
        }

        return new WhatsAppSendResult(idElement.GetString()!);
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
            $"{settings.GraphVersion}/{mediaId}");

        using var response =
            await httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

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

    public async Task<HttpResponseMessage> DownloadMediaAsync(
        Uri mediaUrl,
        CancellationToken cancellationToken)
    {
        if (!mediaUrl.IsAbsoluteUri ||
            mediaUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                "URL de mídia deve usar HTTPS.",
                nameof(mediaUrl));
        }

        var settings = MetaWhatsAppConfiguration.From(configuration);
        using var request = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.AccessToken);

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new HttpRequestException(
                "Falha ao baixar mídia do WhatsApp.",
                null,
                response.StatusCode);
        }

        return response;
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
        string? Id,
        string? VerifiedName,
        string? DisplayPhoneNumber,
        string? QualityRating);

    private sealed record MediaInfoPayload(
        string? Id,
        string? Url,
        string? MimeType,
        string? Sha256,
        long? FileSize);
}

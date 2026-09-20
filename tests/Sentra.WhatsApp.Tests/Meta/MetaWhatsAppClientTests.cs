using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp.Tests.Meta;

public sealed class MetaWhatsAppClientTests
{
    [Fact]
    public async Task GetPhoneInfo_UsesConfiguredGraphVersionAndMapsOfficialFields()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Equal(
                "https://graph.facebook.com/v23.0/123456?fields=id,verified_name,display_phone_number,quality_rating",
                request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("access-token", request.Headers.Authorization?.Parameter);

            return Json("""
                {
                  "id": "123456",
                  "verified_name": "Condomínio Teste",
                  "display_phone_number": "+55 11 99999-9999",
                  "quality_rating": "GREEN"
                }
                """);
        });

        var client = CreateClient(handler);

        var result = await client.GetPhoneInfoAsync(CancellationToken.None);

        Assert.Equal("123456", result.Id);
        Assert.Equal("Condomínio Teste", result.VerifiedName);
        Assert.Equal("+55 11 99999-9999", result.DisplayPhoneNumber);
        Assert.Equal("GREEN", result.QualityRating);
    }

    [Fact]
    public async Task GetMediaInfo_BindsLookupToConfiguredPhoneNumber()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Equal(
                "https://graph.facebook.com/v23.0/998877?phone_number_id=123456",
                request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);

            return Json("""
                {
                  "id": "998877",
                  "url": "https://lookaside.fbsbx.com/whatsapp_business/attachments/example",
                  "mime_type": "audio/ogg",
                  "sha256": "abc123",
                  "file_size": 4096
                }
                """);
        });

        var client = CreateClient(handler);

        var result = await client.GetMediaInfoAsync(
            "998877",
            CancellationToken.None);

        Assert.Equal("998877", result.Id);
        Assert.Equal("audio/ogg", result.MimeType);
        Assert.Equal("abc123", result.Sha256);
        Assert.Equal(4096, result.FileSize);
        Assert.Equal(Uri.UriSchemeHttps, result.Url.Scheme);
    }

    [Fact]
    public async Task SendText_ReturnsWamidAndNeverPlacesTokenInUrl()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(
                "https://graph.facebook.com/v23.0/123456/messages",
                request.RequestUri?.ToString());
            Assert.DoesNotContain(
                "access-token",
                request.RequestUri?.ToString() ?? string.Empty,
                StringComparison.Ordinal);

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains(
                "\"messaging_product\":\"whatsapp\"",
                body,
                StringComparison.Ordinal);
            Assert.Contains(
                "\"to\":\"5511999999999\"",
                body,
                StringComparison.Ordinal);

            return Json("""
                {
                  "messaging_product": "whatsapp",
                  "contacts": [{ "input": "5511999999999", "wa_id": "5511999999999" }],
                  "messages": [{ "id": "wamid.test-123" }]
                }
                """);
        });

        var client = CreateClient(handler);

        var result = await client.SendTextAsync(
            "+5511999999999",
            "Portaria confirmando o recebimento.",
            CancellationToken.None);

        Assert.Equal("wamid.test-123", result.MessageId);
    }

    private static MetaWhatsAppClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://graph.facebook.com/")
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["META_GRAPH_VERSION"] = "v23.0",
                ["META_PHONE_NUMBER_ID"] = "123456",
                ["META_WABA_ID"] = "789012",
                ["META_ACCESS_TOKEN"] = "access-token",
                ["META_VERIFY_TOKEN"] = "verify-token-123456",
                ["META_APP_SECRET"] = "0123456789abcdef0123456789abcdef"
            })
            .Build();

        return new MetaWhatsAppClient(httpClient, configuration);
    }

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        public RecordingHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responder)
            : this(request => Task.FromResult(responder(request)))
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responder(request);
    }
}

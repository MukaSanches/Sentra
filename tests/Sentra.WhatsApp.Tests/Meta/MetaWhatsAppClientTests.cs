using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Sentra.Application.Integrations.WhatsApp;
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
    public async Task GetWabaPhoneNumbers_UsesWabaEndpoint()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Contains(
                "/v23.0/789012/phone_numbers?",
                request.RequestUri?.ToString(),
                StringComparison.Ordinal);

            return Json("""
                {
                  "data": [
                    {
                      "id": "123456",
                      "verified_name": "Condomínio Teste",
                      "display_phone_number": "+55 11 99999-9999",
                      "quality_rating": "GREEN"
                    }
                  ]
                }
                """);
        });

        var client = CreateClient(handler);

        var numbers = await client.GetWabaPhoneNumbersAsync(CancellationToken.None);

        var number = Assert.Single(numbers);
        Assert.Equal("123456", number.Id);
    }

    [Fact]
    public async Task GetTemplates_MapsApprovalMetadata()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Contains(
                "/v23.0/789012/message_templates?",
                request.RequestUri?.ToString(),
                StringComparison.Ordinal);

            return Json("""
                {
                  "data": [
                    {
                      "id": "42",
                      "name": "visitor_notice",
                      "language": "pt_BR",
                      "status": "APPROVED",
                      "category": "UTILITY"
                    }
                  ]
                }
                """);
        });

        var client = CreateClient(handler);

        var templates = await client.GetTemplatesAsync(CancellationToken.None);

        var template = Assert.Single(templates);
        Assert.Equal("APPROVED", template.Status);
        Assert.Equal("visitor_notice", template.Name);
    }

    [Fact]
    public async Task GetFlows_MapsPublishedFlow()
    {
        var handler = new RecordingHandler(request =>
        {
            Assert.Contains(
                "/v23.0/789012/flows?",
                request.RequestUri?.ToString(),
                StringComparison.Ordinal);

            return Json("""
                {
                  "data": [
                    {
                      "id": "555",
                      "name": "Autorização de visitante",
                      "status": "PUBLISHED"
                    }
                  ]
                }
                """);
        });

        var client = CreateClient(handler);

        var flows = await client.GetFlowsAsync(CancellationToken.None);

        var flow = Assert.Single(flows);
        Assert.Equal("555", flow.Id);
        Assert.Equal("PUBLISHED", flow.Status);
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
    public async Task DownloadMedia_UsesFreshMediaUrlAndBearerToken()
    {
        var requests = 0;
        var handler = new RecordingHandler(request =>
        {
            requests++;

            if (requests == 1)
            {
                return Json("""
                    {
                      "id": "998877",
                      "url": "https://lookaside.fbsbx.com/whatsapp_business/attachments/example",
                      "mime_type": "audio/ogg",
                      "sha256": "abc123",
                      "file_size": 4
                    }
                    """);
            }

            Assert.Equal(
                "https://lookaside.fbsbx.com/whatsapp_business/attachments/example",
                request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3, 4])
            };
        });

        var client = CreateClient(handler);
        await using var destination = new MemoryStream();

        await client.DownloadMediaAsync(
            "998877",
            destination,
            CancellationToken.None);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, destination.ToArray());
    }

    [Fact]
    public async Task UploadMedia_UsesOfficialMultipartEndpoint()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(
                "https://graph.facebook.com/v23.0/123456/media",
                request.RequestUri?.ToString());
            Assert.Equal("multipart/form-data", request.Content?.Headers.ContentType?.MediaType);

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("messaging_product", body, StringComparison.Ordinal);
            Assert.Contains("whatsapp", body, StringComparison.Ordinal);
            Assert.Contains("door.jpg", body, StringComparison.Ordinal);

            return Json("""{"id":"778899"}""");
        });

        var client = CreateClient(handler);
        await using var source = new MemoryStream([9, 8, 7]);

        var result = await client.UploadMediaAsync(
            source,
            "door.jpg",
            "image/jpeg",
            CancellationToken.None);

        Assert.Equal("778899", result.MediaId);
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
                ""messaging_product":"whatsapp"",
                body,
                StringComparison.Ordinal);
            Assert.Contains(
                ""to":"5511999999999"",
                body,
                StringComparison.Ordinal);

            return SuccessfulSend();
        });

        var client = CreateClient(handler);

        var result = await client.SendTextAsync(
            "+5511999999999",
            "Portaria confirmando o recebimento.",
            CancellationToken.None);

        Assert.Equal("wamid.test-123", result.MessageId);
    }

    [Fact]
    public async Task SendTemplate_UsesTemplateNameLanguageAndParameters()
    {
        var handler = new RecordingHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();

            Assert.Contains(""type":"template"", body, StringComparison.Ordinal);
            Assert.Contains(""name":"visitor_notice"", body, StringComparison.Ordinal);
            Assert.Contains(""code":"pt_BR"", body, StringComparison.Ordinal);
            Assert.Contains(""text":"Gabriel"", body, StringComparison.Ordinal);

            return SuccessfulSend();
        });

        var client = CreateClient(handler);

        var result = await client.SendTemplateAsync(
            "+5511999999999",
            "visitor_notice",
            "pt_BR",
            ["Gabriel", "19:00"],
            CancellationToken.None);

        Assert.Equal("wamid.test-123", result.MessageId);
    }

    [Fact]
    public async Task SendReplyButtons_LimitsToThreeAndBuildsInteractivePayload()
    {
        var handler = new RecordingHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();

            Assert.Contains(""type":"interactive"", body, StringComparison.Ordinal);
            Assert.Contains(""type":"button"", body, StringComparison.Ordinal);
            Assert.Contains(""title":"Autorizar"", body, StringComparison.Ordinal);

            return SuccessfulSend();
        });

        var client = CreateClient(handler);

        await client.SendReplyButtonsAsync(
            "+5511999999999",
            "Autorizar entrada?",
            [
                new WhatsAppInteractiveButton("approve", "Autorizar"),
                new WhatsAppInteractiveButton("deny", "Negar")
            ],
            CancellationToken.None);
    }

    [Fact]
    public async Task SendFlow_UsesPublishedFlowPayload()
    {
        var handler = new RecordingHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();

            Assert.Contains(""type":"flow"", body, StringComparison.Ordinal);
            Assert.Contains(""flow_id":"555"", body, StringComparison.Ordinal);
            Assert.Contains(""flow_token":"flow-token"", body, StringComparison.Ordinal);
            Assert.Contains(""screen":"VISITOR"", body, StringComparison.Ordinal);

            return SuccessfulSend();
        });

        var client = CreateClient(handler);

        await client.SendFlowAsync(
            "+5511999999999",
            "555",
            "flow-token",
            "Autorizar visitante",
            "Preencha os dados do visitante.",
            "VISITOR",
            new Dictionary<string, string> { ["unit"] = "72" },
            CancellationToken.None);
    }

    [Fact]
    public async Task MarkRead_UsesMessagesEndpointWithPut()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal(
                "https://graph.facebook.com/v23.0/123456/messages",
                request.RequestUri?.ToString());

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains(""status":"read"", body, StringComparison.Ordinal);
            Assert.Contains(""message_id":"wamid.incoming"", body, StringComparison.Ordinal);

            return Json("""{"success":true}""");
        });

        var client = CreateClient(handler);

        await client.MarkMessageReadAsync(
            "wamid.incoming",
            CancellationToken.None);
    }

    [Fact]
    public async Task SendMedia_UsesUploadedMediaId()
    {
        var handler = new RecordingHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();

            Assert.Contains(""type":"document"", body, StringComparison.Ordinal);
            Assert.Contains(""id":"998877"", body, StringComparison.Ordinal);
            Assert.Contains(""filename":"manual.pdf"", body, StringComparison.Ordinal);

            return SuccessfulSend();
        });

        var client = CreateClient(handler);

        await client.SendMediaAsync(
            "+5511999999999",
            WhatsAppMediaKind.Document,
            "998877",
            "Documento da portaria",
            "manual.pdf",
            CancellationToken.None);
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

    private static HttpResponseMessage SuccessfulSend()
        => Json("""
            {
              "messaging_product": "whatsapp",
              "contacts": [{ "input": "5511999999999", "wa_id": "5511999999999" }],
              "messages": [{ "id": "wamid.test-123" }]
            }
            """);

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

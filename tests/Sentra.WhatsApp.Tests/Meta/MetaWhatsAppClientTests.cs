using System.Net;
using System.Text;
using System.Text.Json;
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
            Assert.Equal("test-access", request.Headers.Authorization?.Parameter);

            return Json("""
                {
                  "id": "123456",
                  "verified_name": "Condomínio Teste",
                  "display_phone_number": "+55 11 99999-9999",
                  "quality_rating": "GREEN"
                }
                """);
        });

        var result = await CreateClient(handler)
            .GetPhoneInfoAsync(CancellationToken.None);

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

        var numbers = await CreateClient(handler)
            .GetWabaPhoneNumbersAsync(CancellationToken.None);

        Assert.Equal("123456", Assert.Single(numbers).Id);
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

        var template = Assert.Single(
            await CreateClient(handler)
                .GetTemplatesAsync(CancellationToken.None));

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

        var flow = Assert.Single(
            await CreateClient(handler)
                .GetFlowsAsync(CancellationToken.None));

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

        var result = await CreateClient(handler)
            .GetMediaInfoAsync("998877", CancellationToken.None);

        Assert.Equal("998877", result.Id);
        Assert.Equal("audio/ogg", result.MimeType);
        Assert.Equal(4096, result.FileSize);
    }

    [Fact]
    public async Task DownloadMedia_UsesFreshUrlAndBearerToken()
    {
        var requestNumber = 0;
        var handler = new RecordingHandler(request =>
        {
            requestNumber++;

            if (requestNumber == 1)
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

        await using var destination = new MemoryStream();

        await CreateClient(handler).DownloadMediaAsync(
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
            Assert.Equal(
                "multipart/form-data",
                request.Content?.Headers.ContentType?.MediaType);

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("messaging_product", body, StringComparison.Ordinal);
            Assert.Contains("whatsapp", body, StringComparison.Ordinal);
            Assert.Contains("door.jpg", body, StringComparison.Ordinal);

            return Json("""{"id":"778899"}""");
        });

        await using var source = new MemoryStream([9, 8, 7]);

        var result = await CreateClient(handler).UploadMediaAsync(
            source,
            "door.jpg",
            "image/jpeg",
            CancellationToken.None);

        Assert.Equal("778899", result.MediaId);
    }

    [Fact]
    public async Task SendText_ReturnsWamidAndUsesOfficialEnvelope()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(
                "https://graph.facebook.com/v23.0/123456/messages",
                request.RequestUri?.ToString());

            using var body = await ReadJsonAsync(request);
            var root = body.RootElement;

            Assert.Equal("whatsapp", root.GetProperty("messaging_product").GetString());
            Assert.Equal("5511999999999", root.GetProperty("to").GetString());
            Assert.Equal("text", root.GetProperty("type").GetString());
            Assert.Equal(
                "Portaria confirmando o recebimento.",
                root.GetProperty("text").GetProperty("body").GetString());

            return SuccessfulSend();
        });

        var result = await CreateClient(handler).SendTextAsync(
            "+5511999999999",
            "Portaria confirmando o recebimento.",
            CancellationToken.None);

        Assert.Equal("wamid.test-123", result.MessageId);
    }

    [Fact]
    public async Task SendTemplate_UsesNameLanguageAndBodyParameters()
    {
        var handler = new RecordingHandler(async request =>
        {
            using var body = await ReadJsonAsync(request);
            var template = body.RootElement.GetProperty("template");

            Assert.Equal("template", body.RootElement.GetProperty("type").GetString());
            Assert.Equal("visitor_notice", template.GetProperty("name").GetString());
            Assert.Equal(
                "pt_BR",
                template.GetProperty("language").GetProperty("code").GetString());

            var parameters = template
                .GetProperty("components")[0]
                .GetProperty("parameters");

            Assert.Equal("Gabriel", parameters[0].GetProperty("text").GetString());
            Assert.Equal("19:00", parameters[1].GetProperty("text").GetString());

            return SuccessfulSend();
        });

        await CreateClient(handler).SendTemplateAsync(
            "+5511999999999",
            "visitor_notice",
            "pt_BR",
            ["Gabriel", "19:00"],
            CancellationToken.None);
    }

    [Fact]
    public async Task SendReplyButtons_BuildsInteractivePayload()
    {
        var handler = new RecordingHandler(async request =>
        {
            using var body = await ReadJsonAsync(request);
            var interactive = body.RootElement.GetProperty("interactive");

            Assert.Equal("interactive", body.RootElement.GetProperty("type").GetString());
            Assert.Equal("button", interactive.GetProperty("type").GetString());
            Assert.Equal(
                "Autorizar",
                interactive.GetProperty("action")
                    .GetProperty("buttons")[0]
                    .GetProperty("reply")
                    .GetProperty("title")
                    .GetString());

            return SuccessfulSend();
        });

        await CreateClient(handler).SendReplyButtonsAsync(
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
            using var body = await ReadJsonAsync(request);
            var parameters = body.RootElement
                .GetProperty("interactive")
                .GetProperty("action")
                .GetProperty("parameters");

            Assert.Equal("flow", body.RootElement.GetProperty("interactive").GetProperty("type").GetString());
            Assert.Equal("555", parameters.GetProperty("flow_id").GetString());
            Assert.Equal("flow-token", parameters.GetProperty("flow_token").GetString());
            Assert.Equal(
                "VISITOR",
                parameters.GetProperty("flow_action_payload")
                    .GetProperty("screen")
                    .GetString());

            return SuccessfulSend();
        });

        await CreateClient(handler).SendFlowAsync(
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
    public async Task MarkRead_UsesOfficialPostMessagesEndpoint()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);

            using var body = await ReadJsonAsync(request);
            Assert.Equal("read", body.RootElement.GetProperty("status").GetString());
            Assert.Equal(
                "wamid.incoming",
                body.RootElement.GetProperty("message_id").GetString());

            return Json("""{"success":true}""");
        });

        await CreateClient(handler).MarkMessageReadAsync(
            "wamid.incoming",
            CancellationToken.None);
    }


    [Fact]
    public async Task MarkReadWithTyping_UsesOfficialPostPayload()
    {
        var handler = new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);

            using var body = await ReadJsonAsync(request);
            Assert.Equal("read", body.RootElement.GetProperty("status").GetString());
            Assert.Equal(
                "wamid.incoming",
                body.RootElement.GetProperty("message_id").GetString());
            Assert.Equal(
                "text",
                body.RootElement
                    .GetProperty("typing_indicator")
                    .GetProperty("type")
                    .GetString());

            return Json("""{"success":true}""");
        });

        await CreateClient(handler).MarkMessageReadWithTypingAsync(
            "wamid.incoming",
            CancellationToken.None);
    }

    [Fact]
    public async Task SendMedia_UsesUploadedMediaId()
    {
        var handler = new RecordingHandler(async request =>
        {
            using var body = await ReadJsonAsync(request);

            Assert.Equal("document", body.RootElement.GetProperty("type").GetString());
            var document = body.RootElement.GetProperty("document");
            Assert.Equal("998877", document.GetProperty("id").GetString());
            Assert.Equal("manual.pdf", document.GetProperty("filename").GetString());

            return SuccessfulSend();
        });

        await CreateClient(handler).SendMediaAsync(
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
                ["META_ACCESS_TOKEN"] = string.Concat("test", "-", "access"),
                ["META_VERIFY_TOKEN"] = "verify-value-123456",
                ["META_APP_SECRET"] = new string('a', 32)
            })
            .Build();

        return new MetaWhatsAppClient(httpClient, configuration);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpRequestMessage request)
        => JsonDocument.Parse(await request.Content!.ReadAsStringAsync());

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

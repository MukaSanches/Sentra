using System.Net;
using System.Net.Http;
using System.Text;
using Sentra.Contracts.WhatsApp;
using Sentra.WhatsApp.Configuration;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp.Tests.Meta;

public sealed class MetaWhatsAppClientTests
{
    [Fact]
    public async Task SendText_UsesConfiguredGraphEndpointAndBearer()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"messages":[{"id":"wamid.123"}]}""", Encoding.UTF8, "application/json")
        });
        var client = Create(handler);

        var result = await client.SendTextAsync(
            "5511999999999",
            "Olá",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal("wamid.123", result.MessageId);
        Assert.Equal("https://graph.facebook.com/v99.0/phone-1/messages", handler.Uri?.ToString());
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("access-token-test", handler.AuthorizationParameter);
        Assert.Contains(""messaging_product":"whatsapp"", handler.Body);
        Assert.Contains(""type":"text"", handler.Body);
    }

    [Fact]
    public async Task GetPhoneNumbers_MapsOfficialResponse()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"data":[{"verified_name":"SENTRA","display_phone_number":"+55 11 99999-9999","id":"phone-1","quality_rating":"GREEN"}]}""",
                Encoding.UTF8,
                "application/json")
        });
        var client = Create(handler);

        var values = await client.GetPhoneNumbersAsync(TestContext.Current.CancellationToken);

        var item = Assert.Single(values);
        Assert.Equal("phone-1", item.Id);
        Assert.Equal("GREEN", item.QualityRating);
        Assert.Equal("https://graph.facebook.com/v99.0/waba-1/phone_numbers", handler.Uri?.ToString());
    }

    [Fact]
    public async Task SendButtons_RejectsMoreThanThreeButtons()
    {
        var client = Create(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.SendButtonsAsync(
            "5511999999999",
            "Escolha",
            [
                new("1", "Um"),
                new("2", "Dois"),
                new("3", "Três"),
                new("4", "Quatro")
            ],
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MarkRead_UsesPutMessagesEndpoint()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"success":true}""", Encoding.UTF8, "application/json")
        });
        var client = Create(handler);

        Assert.True(await client.MarkReadAsync("wamid.in", TestContext.Current.CancellationToken));
        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.Contains(""status":"read"", handler.Body);
    }

    private static MetaWhatsAppClient Create(RecordingHandler handler)
        => new(
            new FakeFactory(new HttpClient(handler)),
            new WhatsAppOptions
            {
                GraphApiVersion = "v99.0",
                AppId = "app-1",
                WabaId = "waba-1",
                PhoneNumberId = "phone-1",
                AccessToken = "access-token-test",
                AppSecret = "secret",
                VerifyToken = "verify",
                PublicBaseUrl = "https://sentra.example.test"
            });

    private sealed class FakeFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            Method = request.Method;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            if (request.Content is not null)
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }
}

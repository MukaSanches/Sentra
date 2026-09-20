using System.Net;
using System.Net.Http;
using System.Text;
using Sentra.Desktop.Models;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.Tests.Services;

public sealed class SentraApiClientTests
{
    [Fact]
    public async Task GetBlocks_SendsBearerAndCondominiumRoute()
    {
        var condominiumId = Guid.NewGuid();
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"items":[],"page":1,"pageSize":50,"total":0}""",
                    Encoding.UTF8,
                    "application/json")
            });
        var client = new SentraApiClient(
            new FakeHttpClientFactory(new HttpClient(handler)),
            new FixedSettingsService(new DesktopSettings("https://sentra.example.test", condominiumId)),
            new FixedTokenProvider("token-value"));

        var result = await client.GetBlocksAsync();

        Assert.Empty(result.Items);
        Assert.Equal(
            $"https://sentra.example.test/api/condominiums/{condominiumId:D}/blocks?page=1&pageSize=50",
            handler.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("token-value", handler.AuthorizationParameter);
    }

    [Fact]
    public async Task GetBlocks_MapsUnauthorizedToSessionMessage()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new SentraApiClient(
            new FakeHttpClientFactory(new HttpClient(handler)),
            new FixedSettingsService(new DesktopSettings("https://sentra.example.test", Guid.NewGuid())),
            new FixedTokenProvider(null));

        var exception = await Assert.ThrowsAsync<SentraApiException>(() => client.GetBlocksAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("Sessão", exception.Message, StringComparison.Ordinal);
    }

    private sealed class FixedSettingsService(DesktopSettings settings) : IDesktopSettingsService
    {
        public Task<DesktopSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(settings);

        public Task SaveAsync(DesktopSettings value, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FixedTokenProvider(string? token) : IAccessTokenProvider
    {
        public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(token);
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return Task.FromResult(response);
        }
    }
}

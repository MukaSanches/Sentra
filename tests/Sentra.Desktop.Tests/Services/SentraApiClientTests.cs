using System.Net;
using System.Net.Http;
using System.Text;
using Sentra.Contracts.Auth;
using Sentra.Desktop.Models;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.Tests.Services;

public sealed class SentraApiClientTests
{
    [Fact]
    public async Task GetConversations_UsesServerUrlAndBearerSession()
    {
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "[]",
                    Encoding.UTF8,
                    "application/json")
            });

        var session = ValidSession();
        var client = new SentraApiClient(
            new FixedHttpClientFactory(new HttpClient(handler)),
            new FixedSettingsService(
                new DesktopSettings(
                    "https://sentra.example.test",
                    session.Employee!.CondominiumId,
                    "porteiro")),
            session);

        var result = await client.GetConversationsAsync(
            TestContext.Current.CancellationToken);

        Assert.Empty(result);
        Assert.Equal(
            "https://sentra.example.test/api/v1/conversations",
            handler.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("jwt-test-value", handler.AuthorizationParameter);
    }

    [Fact]
    public async Task GetConversations_RefusesWithoutSession()
    {
        var client = new SentraApiClient(
            new FixedHttpClientFactory(new HttpClient(new RecordingHandler(
                new HttpResponseMessage(HttpStatusCode.OK)))),
            new FixedSettingsService(
                new DesktopSettings(
                    "https://sentra.example.test",
                    Guid.NewGuid(),
                    "porteiro")),
            new SessionState());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetConversationsAsync(
                TestContext.Current.CancellationToken));
    }

    private static SessionState ValidSession()
    {
        var state = new SessionState();
        state.Set(new LoginResponse(
            "jwt-test-value",
            DateTimeOffset.UtcNow.AddMinutes(10),
            new EmployeeIdentityResponse(
                Guid.NewGuid(),
                "Porteiro",
                "porteiro",
                Guid.NewGuid(),
                Guid.NewGuid()),
            ["conversations.read"]));
        return state;
    }

    private sealed class FixedSettingsService(
        DesktopSettings settings) : IDesktopSettingsService
    {
        public Task<DesktopSettings> LoadAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(settings);

        public Task SaveAsync(
            DesktopSettings value,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FixedHttpClientFactory(
        HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(
        HttpResponseMessage response) : HttpMessageHandler
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

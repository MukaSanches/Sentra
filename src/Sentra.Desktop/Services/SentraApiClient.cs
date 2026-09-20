using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sentra.Contracts.Auth;
using Sentra.Contracts.Setup;
using Sentra.Contracts.WhatsApp;

namespace Sentra.Desktop.Services;

public sealed class SentraApiClient(
    IHttpClientFactory httpClientFactory,
    IDesktopSettingsService settingsService,
    SessionState sessionState) : ISentraApiClient
{
    public async Task<bool> IsAliveAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = await CreateRequestAsync(
                HttpMethod.Get,
                "health/live",
                authenticated: false,
                cancellationToken);
            using var response = await SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public Task<SetupStatusResponse> GetSetupStatusAsync(
        CancellationToken cancellationToken = default)
        => GetAsync<SetupStatusResponse>(
            "api/v1/setup/status",
            authenticated: false,
            cancellationToken);

    public async Task<BootstrapResponse> BootstrapAsync(
        BootstrapRequest request,
        string bootstrapToken,
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            "api/v1/setup/bootstrap",
            authenticated: false,
            cancellationToken);
        message.Headers.Add("X-Sentra-Bootstrap-Token", bootstrapToken);
        message.Content = JsonContent.Create(request);
        return await SendForJsonAsync<BootstrapResponse>(
            message,
            cancellationToken);
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            "api/v1/auth/login",
            authenticated: false,
            cancellationToken);
        message.Content = JsonContent.Create(request);

        var response = await SendForJsonAsync<LoginResponse>(
            message,
            cancellationToken);

        sessionState.Set(response);
        return response;
    }

    public Task<WhatsAppIntegrationStatusResponse> GetWhatsAppStatusAsync(
        CancellationToken cancellationToken = default)
        => GetAsync<WhatsAppIntegrationStatusResponse>(
            "api/v1/integrations/whatsapp/status",
            authenticated: true,
            cancellationToken);

    public async Task<WhatsAppIntegrationVerifyResponse> VerifyWhatsAppAsync(
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            "api/v1/integrations/whatsapp/verify",
            authenticated: true,
            cancellationToken);
        return await SendForJsonAsync<WhatsAppIntegrationVerifyResponse>(
            message,
            cancellationToken);
    }

    public async Task<IReadOnlyList<WhatsAppTemplateResponse>> GetTemplatesAsync(
        CancellationToken cancellationToken = default)
        => await GetAsync<List<WhatsAppTemplateResponse>>(
            "api/v1/integrations/whatsapp/templates",
            authenticated: true,
            cancellationToken);

    public async Task<IReadOnlyList<WhatsAppFlowResponse>> GetFlowsAsync(
        CancellationToken cancellationToken = default)
        => await GetAsync<List<WhatsAppFlowResponse>>(
            "api/v1/integrations/whatsapp/flows",
            authenticated: true,
            cancellationToken);

    public async Task<IReadOnlyList<ConversationSummaryResponse>> GetConversationsAsync(
        CancellationToken cancellationToken = default)
        => await GetAsync<List<ConversationSummaryResponse>>(
            "api/v1/conversations",
            authenticated: true,
            cancellationToken);

    public async Task<IReadOnlyList<ConversationMessageResponse>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
        => await GetAsync<List<ConversationMessageResponse>>(
            $"api/v1/conversations/{conversationId:D}/messages",
            authenticated: true,
            cancellationToken);

    public async Task<IReadOnlyList<ConversationAttachmentResponse>> GetAttachmentsAsync(
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken = default)
        => await GetAsync<List<ConversationAttachmentResponse>>(
            $"api/v1/conversations/{conversationId:D}/messages/{messageId:D}/attachments",
            authenticated: true,
            cancellationToken);

    public async Task<ConversationMessageResponse> SendTextAsync(
        Guid conversationId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var request = new SendWhatsAppTextRequest(Guid.NewGuid(), text);
        return await PostJsonAsync<SendWhatsAppTextRequest, ConversationMessageResponse>(
            $"api/v1/conversations/{conversationId:D}/messages/text",
            request,
            cancellationToken);
    }

    public Task<ConversationMessageResponse> SendTemplateAsync(
        Guid conversationId,
        SendWhatsAppTemplateRequest request,
        CancellationToken cancellationToken = default)
        => PostJsonAsync<SendWhatsAppTemplateRequest, ConversationMessageResponse>(
            $"api/v1/conversations/{conversationId:D}/messages/template",
            request,
            cancellationToken);

    public Task<ConversationMessageResponse> SendButtonsAsync(
        Guid conversationId,
        SendWhatsAppReplyButtonsRequest request,
        CancellationToken cancellationToken = default)
        => PostJsonAsync<SendWhatsAppReplyButtonsRequest, ConversationMessageResponse>(
            $"api/v1/conversations/{conversationId:D}/messages/buttons",
            request,
            cancellationToken);

    public Task<ConversationMessageResponse> SendFlowAsync(
        Guid conversationId,
        SendWhatsAppFlowRequest request,
        CancellationToken cancellationToken = default)
        => PostJsonAsync<SendWhatsAppFlowRequest, ConversationMessageResponse>(
            $"api/v1/conversations/{conversationId:D}/messages/flow",
            request,
            cancellationToken);

    public async Task<ConversationMessageResponse> SendMediaAsync(
        Guid conversationId,
        string kind,
        Stream content,
        string fileName,
        string contentType,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/v1/conversations/{conversationId:D}/messages/media",
            authenticated: true,
            cancellationToken);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Guid.NewGuid().ToString()), "clientRequestId");
        form.Add(new StringContent(kind), "kind");

        if (!string.IsNullOrWhiteSpace(caption))
        {
            form.Add(new StringContent(caption), "caption");
        }

        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);
        message.Content = form;

        return await SendForJsonAsync<ConversationMessageResponse>(
            message,
            cancellationToken);
    }

    public async Task MarkReadAsync(
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/v1/conversations/{conversationId:D}/messages/{messageId:D}/read",
            authenticated: true,
            cancellationToken);
        using var response = await SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DownloadAttachmentAsync(
        Guid conversationId,
        Guid attachmentId,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Get,
            $"api/v1/conversations/{conversationId:D}/attachments/{attachmentId:D}/content",
            authenticated: true,
            cancellationToken);
        using var response = await SendAsync(
            message,
            cancellationToken,
            HttpCompletionOption.ResponseHeadersRead);
        await EnsureSuccessAsync(response, cancellationToken);
        await response.Content.CopyToAsync(destination, cancellationToken);
    }

    private async Task<T> PostJsonAsync<TRequest, T>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            path,
            authenticated: true,
            cancellationToken);
        message.Content = JsonContent.Create(payload);
        return await SendForJsonAsync<T>(message, cancellationToken);
    }

    private async Task<T> GetAsync<T>(
        string path,
        bool authenticated,
        CancellationToken cancellationToken)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Get,
            path,
            authenticated,
            cancellationToken);
        return await SendForJsonAsync<T>(message, cancellationToken);
    }

    private async Task<T> SendForJsonAsync<T>(
        HttpRequestMessage message,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "O servidor SENTRA retornou uma resposta vazia ou inválida.");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string path,
        bool authenticated,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        var baseUri = settings.TryGetApiBaseUri()
            ?? throw new InvalidOperationException(
                "Configure a URL do servidor SENTRA.");

        var message = new HttpRequestMessage(
            method,
            new Uri(baseUri, path.TrimStart('/')));

        if (authenticated)
        {
            var token = sessionState.AccessToken
                ?? throw new InvalidOperationException(
                    "Faça login no SENTRA antes de continuar.");

            message.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return message;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage message,
        CancellationToken cancellationToken,
        HttpCompletionOption completionOption =
            HttpCompletionOption.ResponseContentRead)
    {
        var client = httpClientFactory.CreateClient("SENTRA");
        return await client.SendAsync(
            message,
            completionOption,
            cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                "Sessão inválida, expirada ou credencial incorreta.",
            HttpStatusCode.Forbidden =>
                "Seu usuário não possui permissão para esta operação.",
            HttpStatusCode.NotFound =>
                "O recurso solicitado não foi encontrado.",
            HttpStatusCode.Conflict =>
                "A operação entrou em conflito com o estado atual.",
            _ when string.IsNullOrWhiteSpace(body) =>
                $"Servidor retornou HTTP {(int)response.StatusCode}.",
            _ => body
        };

        throw new SentraApiException(response.StatusCode, message);
    }
}

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sentra.Contracts.Admin;
using Sentra.Contracts.Auth;
using Sentra.Contracts.Core;
using Sentra.Contracts.Operations;
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

    public Task<WhatsAppConfigurationStatusResponse> GetWhatsAppConfigurationAsync(
        CancellationToken cancellationToken = default)
        => GetAsync<WhatsAppConfigurationStatusResponse>(
            "api/v1/integrations/whatsapp/configuration",
            authenticated: true,
            cancellationToken);




    public async Task ExportBackupAsync(
        string passphrase,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Get,
            "api/v1/admin/backup",
            authenticated: true,
            cancellationToken);
        message.Headers.Add("X-Sentra-Backup-Passphrase", passphrase);

        using var response = await SendAsync(
            message,
            cancellationToken,
            HttpCompletionOption.ResponseHeadersRead);
        await EnsureSuccessAsync(response, cancellationToken);
        await response.Content.CopyToAsync(destination, cancellationToken);
    }

    public async Task RestoreBackupAsync(
        string passphrase,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            "api/v1/admin/backup/restore",
            authenticated: true,
            cancellationToken);
        message.Headers.Add("X-Sentra-Backup-Passphrase", passphrase);

        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.sentra.backup");
        form.Add(streamContent, "file", fileName);
        message.Content = form;

        using var response = await SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<ImportAdminResponse> ImportUnitsAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
        => ImportFileAsync("api/v1/admin/import/units", content, fileName, cancellationToken);

    public Task<ImportAdminResponse> ImportResidentsAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
        => ImportFileAsync("api/v1/admin/import/residents", content, fileName, cancellationToken);

    private async Task<ImportAdminResponse> ImportFileAsync(
        string path,
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            path,
            authenticated: true,
            cancellationToken);

        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv");
        form.Add(streamContent, "file", fileName);
        message.Content = form;

        return await SendForJsonAsync<ImportAdminResponse>(message, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionAdminResponse>> GetAdminPermissionsAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<PermissionAdminResponse>>("api/v1/admin/permissions", true, cancellationToken);

    public async Task<IReadOnlyList<RoleAdminResponse>> GetAdminRolesAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<RoleAdminResponse>>("api/v1/admin/roles", true, cancellationToken);

    public Task<RoleAdminResponse> CreateAdminRoleAsync(CreateRoleAdminRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<CreateRoleAdminRequest, RoleAdminResponse>("api/v1/admin/roles", request, cancellationToken);

    public async Task<IReadOnlyList<EmployeeAdminResponse>> GetAdminEmployeesAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<EmployeeAdminResponse>>("api/v1/admin/employees", true, cancellationToken);

    public Task<EmployeeAdminResponse> CreateAdminEmployeeAsync(CreateEmployeeAdminRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<CreateEmployeeAdminRequest, EmployeeAdminResponse>("api/v1/admin/employees", request, cancellationToken);

    public async Task<IReadOnlyList<UnitResponse>> GetUnitsAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<UnitResponse>>("api/v1/units", true, cancellationToken);

    public async Task<IReadOnlyList<ResidentResponse>> GetResidentsAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<ResidentResponse>>("api/v1/residents", true, cancellationToken);

    public Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
        => GetAsync<DashboardResponse>("api/v1/dashboard", true, cancellationToken);

    public async Task<IReadOnlyList<VisitorAuthorizationResponse>> GetVisitorAuthorizationsAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
        => await GetAsync<List<VisitorAuthorizationResponse>>($"api/v1/visitors/authorizations?activeOnly={activeOnly.ToString().ToLowerInvariant()}", true, cancellationToken);

    public Task<VisitorAuthorizationResponse> CreateVisitorAuthorizationAsync(CreateVisitorAuthorizationRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<CreateVisitorAuthorizationRequest, VisitorAuthorizationResponse>("api/v1/visitors/authorizations", request, cancellationToken);

    public Task<VisitorAuthorizationResponse> RegisterVisitAsync(Guid authorizationId, string action, CancellationToken cancellationToken = default)
        => PostJsonAsync<RegisterVisitRequest, VisitorAuthorizationResponse>($"api/v1/visitors/authorizations/{authorizationId:D}/visit", new RegisterVisitRequest(action), cancellationToken);

    public async Task<QrCredentialResponse> CreateQrAsync(Guid authorizationId, CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(HttpMethod.Post, $"api/v1/visitors/authorizations/{authorizationId:D}/qr", true, cancellationToken);
        return await SendForJsonAsync<QrCredentialResponse>(message, cancellationToken);
    }

    public Task<ValidateQrResponse> ValidateQrAsync(string payload, CancellationToken cancellationToken = default)
        => PostJsonAsync<ValidateQrRequest, ValidateQrResponse>("api/v1/visitors/qr/validate", new ValidateQrRequest(payload), cancellationToken);

    public async Task<IReadOnlyList<ServiceProviderResponse>> GetProvidersAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<ServiceProviderResponse>>("api/v1/providers", true, cancellationToken);

    public Task<ServiceProviderResponse> CreateProviderAsync(CreateServiceProviderRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<CreateServiceProviderRequest, ServiceProviderResponse>("api/v1/providers", request, cancellationToken);

    public Task<ProviderAuthorizationResponse> CreateProviderAuthorizationAsync(CreateProviderAuthorizationRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<CreateProviderAuthorizationRequest, ProviderAuthorizationResponse>("api/v1/providers/authorizations", request, cancellationToken);

    public async Task<IReadOnlyList<PackageResponse>> GetPackagesAsync(bool pendingOnly = true, CancellationToken cancellationToken = default)
        => await GetAsync<List<PackageResponse>>($"api/v1/packages?pendingOnly={pendingOnly.ToString().ToLowerInvariant()}", true, cancellationToken);

    public Task<PackageResponse> CreatePackageAsync(CreatePackageRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<CreatePackageRequest, PackageResponse>("api/v1/packages", request, cancellationToken);

    public Task<PackageResponse> CollectPackageAsync(Guid packageId, CancellationToken cancellationToken = default)
        => PostJsonAsync<CollectPackageRequest, PackageResponse>($"api/v1/packages/{packageId:D}/collect", new CollectPackageRequest(null), cancellationToken);

    public async Task<IReadOnlyList<OccurrenceResponse>> GetOccurrencesAsync(bool openOnly = true, CancellationToken cancellationToken = default)
        => await GetAsync<List<OccurrenceResponse>>($"api/v1/occurrences?openOnly={openOnly.ToString().ToLowerInvariant()}", true, cancellationToken);

    public Task<OccurrenceResponse> CreateOccurrenceAsync(CreateOccurrenceRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<CreateOccurrenceRequest, OccurrenceResponse>("api/v1/occurrences", request, cancellationToken);

    public async Task<OccurrenceResponse> UpdateOccurrenceAsync(Guid occurrenceId, UpdateOccurrenceRequest request, CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(HttpMethod.Put, $"api/v1/occurrences/{occurrenceId:D}", true, cancellationToken);
        message.Content = JsonContent.Create(request);
        return await SendForJsonAsync<OccurrenceResponse>(message, cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftResponse>> GetShiftsAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<ShiftResponse>>("api/v1/shifts", true, cancellationToken);

    public Task<ShiftResponse> OpenShiftAsync(CancellationToken cancellationToken = default)
        => PostJsonAsync<OpenShiftRequest, ShiftResponse>("api/v1/shifts/open", new OpenShiftRequest(), cancellationToken);

    public Task<ShiftResponse> CloseShiftAsync(Guid shiftId, string summary, CancellationToken cancellationToken = default)
        => PostJsonAsync<CloseShiftRequest, ShiftResponse>($"api/v1/shifts/{shiftId:D}/close", new CloseShiftRequest(summary), cancellationToken);

    public Task<ShiftResponse> AcknowledgeShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
        => PostJsonAsync<AcknowledgeShiftRequest, ShiftResponse>($"api/v1/shifts/{shiftId:D}/acknowledge", new AcknowledgeShiftRequest(), cancellationToken);

    public async Task<IReadOnlyList<AnnouncementResponse>> GetAnnouncementsAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<AnnouncementResponse>>("api/v1/announcements", true, cancellationToken);

    public Task<AnnouncementResponse> CreateAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken cancellationToken = default)
        => PostJsonAsync<CreateAnnouncementRequest, AnnouncementResponse>("api/v1/announcements", request, cancellationToken);

    public async Task<IReadOnlyList<SearchResultResponse>> SearchAsync(string query, CancellationToken cancellationToken = default)
        => await GetAsync<List<SearchResultResponse>>($"api/v1/search?q={Uri.EscapeDataString(query)}", true, cancellationToken);

    public Task<OfflineSnapshotResponse> GetOfflineSnapshotAsync(CancellationToken cancellationToken = default)
        => GetAsync<OfflineSnapshotResponse>("api/v1/offline/snapshot", true, cancellationToken);

    public async Task<IReadOnlyList<AuditRecordResponse>> GetAuditAsync(int take = 200, CancellationToken cancellationToken = default)
        => await GetAsync<List<AuditRecordResponse>>($"api/v1/audit?take={Math.Clamp(take,1,1000)}", true, cancellationToken);

    public async Task<IntelligenceAnalysisResponse> AnalyzeConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(HttpMethod.Post, $"api/v1/conversations/{conversationId:D}/intelligence/analyze", true, cancellationToken);
        return await SendForJsonAsync<IntelligenceAnalysisResponse>(message, cancellationToken);
    }

    public Task<PendingActionResponse> ResolvePendingActionAsync(Guid pendingActionId, string decision, CancellationToken cancellationToken = default)
        => PostJsonAsync<ResolvePendingActionRequest, PendingActionResponse>($"api/v1/pending-actions/{pendingActionId:D}/resolve", new ResolvePendingActionRequest(decision), cancellationToken);

    public async Task SendOutboxAsync(string method, string path, string? jsonBody, CancellationToken cancellationToken = default)
    {
        var httpMethod = new HttpMethod(method);
        using var message = await CreateRequestAsync(httpMethod, path, true, cancellationToken);
        if (!string.IsNullOrWhiteSpace(jsonBody))
        {
            message.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        }
        using var response = await SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
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

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sentra.Contracts.Common;
using Sentra.Contracts.Operations;

namespace Sentra.Desktop.Services;

public sealed class SentraApiClient(
    IHttpClientFactory httpClientFactory,
    IDesktopSettingsService settingsService,
    IAccessTokenProvider accessTokenProvider) : ISentraApiClient
{
    public async Task<bool> IsServerAliveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "/health/live", requireCondominium: false, cancellationToken);
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

    public Task<SetupStatusResponse> GetSetupStatusAsync(CancellationToken cancellationToken = default)
        => GetAsync<SetupStatusResponse>("/api/setup/status", requireCondominium: false, cancellationToken);

    public Task<PagedResponse<BlockResponse>> GetBlocksAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
        => GetForCondominiumAsync<PagedResponse<BlockResponse>>(
            $"/blocks?page={page}&pageSize={pageSize}",
            cancellationToken);

    public Task<BlockResponse> CreateBlockAsync(
        CreateBlockRequest request,
        CancellationToken cancellationToken = default)
        => PostForCondominiumAsync<CreateBlockRequest, BlockResponse>("/blocks", request, cancellationToken);

    public Task<PagedResponse<UnitResponse>> GetUnitsAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
        => GetForCondominiumAsync<PagedResponse<UnitResponse>>(
            $"/units?page={page}&pageSize={pageSize}",
            cancellationToken);

    public Task<UnitResponse> CreateUnitAsync(
        CreateUnitRequest request,
        CancellationToken cancellationToken = default)
        => PostForCondominiumAsync<CreateUnitRequest, UnitResponse>("/units", request, cancellationToken);

    public Task<PagedResponse<ResidentResponse>> GetResidentsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var encodedSearch = string.IsNullOrWhiteSpace(search)
            ? string.Empty
            : $"&search={Uri.EscapeDataString(search.Trim())}";
        return GetForCondominiumAsync<PagedResponse<ResidentResponse>>(
            $"/residents?page={page}&pageSize={pageSize}{encodedSearch}",
            cancellationToken);
    }

    public Task<ResidentResponse> CreateResidentAsync(
        CreateResidentRequest request,
        CancellationToken cancellationToken = default)
        => PostForCondominiumAsync<CreateResidentRequest, ResidentResponse>("/residents", request, cancellationToken);

    public Task<PagedResponse<EmployeeResponse>> GetEmployeesAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
        => GetForCondominiumAsync<PagedResponse<EmployeeResponse>>(
            $"/employees?page={page}&pageSize={pageSize}",
            cancellationToken);

    public async Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken = default)
        => await GetForCondominiumAsync<List<RoleResponse>>("/roles", cancellationToken);

    public Task<RoleResponse> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
        => PostForCondominiumAsync<CreateRoleRequest, RoleResponse>("/roles", request, cancellationToken);

    private async Task<T> GetForCondominiumAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        var condominiumId = settings.CondominiumId
            ?? throw new InvalidOperationException("Condomínio ainda não configurado no cliente.");
        return await GetAsync<T>($"/api/condominiums/{condominiumId:D}{path}", true, cancellationToken);
    }

    private async Task<TResponse> PostForCondominiumAsync<TRequest, TResponse>(
        string path,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        var condominiumId = settings.CondominiumId
            ?? throw new InvalidOperationException("Condomínio ainda não configurado no cliente.");

        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"/api/condominiums/{condominiumId:D}{path}",
            requireCondominium: true,
            cancellationToken);
        request.Content = JsonContent.Create(payload);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia ou inválida do servidor SENTRA.");
    }

    private async Task<T> GetAsync<T>(
        string path,
        bool requireCondominium,
        CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, path, requireCondominium, cancellationToken);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia ou inválida do servidor SENTRA.");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string path,
        bool requireCondominium,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        if (!settings.HasServerConfiguration)
        {
            throw new InvalidOperationException("Servidor SENTRA ainda não configurado.");
        }

        if (requireCondominium && settings.CondominiumId is null)
        {
            throw new InvalidOperationException("Condomínio ainda não configurado no cliente.");
        }

        var baseUri = new Uri(settings.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var request = new HttpRequestMessage(method, new Uri(baseUri, path.TrimStart('/')));

        var token = await accessTokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("SENTRA");
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
            HttpStatusCode.Unauthorized => "Sessão não autenticada ou expirada.",
            HttpStatusCode.Forbidden => "Usuário sem permissão para esta operação.",
            HttpStatusCode.NotFound => "Recurso não encontrado no SENTRA.",
            _ => string.IsNullOrWhiteSpace(body)
                ? $"Servidor SENTRA retornou HTTP {(int)response.StatusCode}."
                : body
        };

        throw new SentraApiException(response.StatusCode, message);
    }
}

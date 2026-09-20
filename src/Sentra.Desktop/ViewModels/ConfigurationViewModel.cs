using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.Operations;
using Sentra.Desktop.Models;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class ConfigurationViewModel(
    IDesktopSettingsService settingsService,
    ISentraApiClient apiClient,
    IAccessTokenProvider accessTokenProvider)
    : PageViewModel("Configurações")
{
    [ObservableProperty]
    private string _apiBaseUrl = string.Empty;

    [ObservableProperty]
    private string _condominiumIdText = string.Empty;

    [ObservableProperty]
    private string _condominiumName = string.Empty;

    [ObservableProperty]
    private string _administratorName = string.Empty;

    [ObservableProperty]
    private string _serverStatus = "AGUARDANDO CONFIGURAÇÃO";

    [ObservableProperty]
    private string _authenticationStatus = "AGUARDANDO CONFIGURAÇÃO";

    [ObservableProperty]
    private string _setupStatus = "NÃO VERIFICADO";

    [ObservableProperty]
    private string _status = "Configure a URL real do backend SENTRA.";

    public async Task LoadAsync()
    {
        var settings = await settingsService.LoadAsync();
        ApiBaseUrl = settings.ApiBaseUrl;
        CondominiumIdText = settings.CondominiumId?.ToString() ?? string.Empty;

        var token = await accessTokenProvider.GetAccessTokenAsync();
        AuthenticationStatus = string.IsNullOrWhiteSpace(token)
            ? "AGUARDANDO CONFIGURAÇÃO"
            : "CREDENCIAL DE SESSÃO DISPONÍVEL";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            Guid? condominiumId = null;
            if (!string.IsNullOrWhiteSpace(CondominiumIdText))
            {
                if (!Guid.TryParse(CondominiumIdText, out var parsed))
                {
                    Status = "ID do condomínio inválido.";
                    return;
                }

                condominiumId = parsed;
            }

            await settingsService.SaveAsync(new DesktopSettings(ApiBaseUrl.Trim(), condominiumId));
            Status = "Configuração local salva. Isso não significa que o servidor está conectado.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task TestAsync()
    {
        await SaveAsync();

        try
        {
            ServerStatus = await apiClient.IsServerAliveAsync() ? "ONLINE" : "INDISPONÍVEL";
            if (ServerStatus != "ONLINE")
            {
                SetupStatus = "NÃO VERIFICADO";
                Status = "O health check real do servidor não respondeu.";
                return;
            }

            var setup = await apiClient.GetSetupStatusAsync();
            SetupStatus = setup.IsInitialized ? "INICIALIZADO" : "CONFIGURAÇÃO INICIAL PENDENTE";
            Status = "Teste real concluído.";
        }
        catch (Exception exception)
        {
            ServerStatus = "ERRO";
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task BootstrapAsync()
    {
        try
        {
            var token = await accessTokenProvider.GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                AuthenticationStatus = "AGUARDANDO CONFIGURAÇÃO";
                Status = "A configuração inicial exige uma sessão autenticada real.";
                return;
            }

            var result = await apiClient.BootstrapAsync(
                new BootstrapRequest(CondominiumName, AdministratorName));
            CondominiumIdText = result.Id.ToString();
            await settingsService.SaveAsync(new DesktopSettings(ApiBaseUrl.Trim(), result.Id));
            SetupStatus = "INICIALIZADO";
            Status = "Condomínio e administrador criados no servidor.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }
}

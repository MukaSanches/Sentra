using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.Auth;
using Sentra.Contracts.Setup;
using Sentra.Desktop.Models;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class ConnectionViewModel(
    IDesktopSettingsService settingsService,
    ISentraApiClient apiClient,
    SessionState sessionState,
    RealtimeService realtimeService)
    : PageViewModel("Conexão e acesso")
{
    public event EventHandler? Authenticated;

    [ObservableProperty]
    private string _apiBaseUrl = string.Empty;

    [ObservableProperty]
    private string _condominiumIdText = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _serverStatus = "NÃO VERIFICADO";

    [ObservableProperty]
    private string _setupStatus = "NÃO VERIFICADO";

    [ObservableProperty]
    private string _authenticationStatus = "DESCONECTADO";

    [ObservableProperty]
    private string _status = "Configure o servidor SENTRA.";

    public async Task InitializeAsync()
    {
        var settings = await settingsService.LoadAsync();
        ApiBaseUrl = settings.ApiBaseUrl;
        CondominiumIdText = settings.CondominiumId?.ToString() ?? string.Empty;
        Username = settings.Username ?? string.Empty;

        await TestServerAsync();
    }

    [RelayCommand]
    public async Task SaveAsync()
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

            await settingsService.SaveAsync(
                new DesktopSettings(
                    ApiBaseUrl.Trim(),
                    condominiumId,
                    string.IsNullOrWhiteSpace(Username)
                        ? null
                        : Username.Trim()));

            Status = "Configuração local salva. Senha e token de sessão não são gravados.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    public async Task TestServerAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                ServerStatus = "AGUARDANDO CONFIGURAÇÃO";
                SetupStatus = "NÃO VERIFICADO";
                return;
            }

            await SaveAsync();

            ServerStatus = await apiClient.IsAliveAsync()
                ? "ONLINE"
                : "INDISPONÍVEL";

            if (ServerStatus != "ONLINE")
            {
                SetupStatus = "NÃO VERIFICADO";
                return;
            }

            var setup = await apiClient.GetSetupStatusAsync();
            SetupStatus = setup.Bootstrapped
                ? "INICIALIZADO"
                : "CONFIGURAÇÃO INICIAL NECESSÁRIA";
        }
        catch (Exception exception)
        {
            ServerStatus = "ERRO";
            Status = DescribeError(exception);
        }
    }

    public async Task LoginAsync(string password)
    {
        try
        {
            if (!Guid.TryParse(CondominiumIdText, out var condominiumId))
            {
                Status = "Informe o ID válido do condomínio.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrEmpty(password))
            {
                Status = "Usuário e senha são obrigatórios.";
                return;
            }

            await SaveAsync();

            var result = await apiClient.LoginAsync(
                new LoginRequest(
                    condominiumId,
                    Username.Trim(),
                    password));

            AuthenticationStatus =
                $"CONECTADO • {result.Employee.FullName}";
            Status = "Sessão autenticada. O token permanece somente em memória.";

            await realtimeService.ConnectAsync();
            Authenticated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            sessionState.Clear();
            AuthenticationStatus = "DESCONECTADO";
            Status = DescribeError(exception);
        }
    }

    public async Task BootstrapAsync(
        string condominiumName,
        string administratorName,
        string username,
        string password,
        string bootstrapToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bootstrapToken))
            {
                Status = "O token de bootstrap do servidor é obrigatório.";
                return;
            }

            await SaveAsync();

            var result = await apiClient.BootstrapAsync(
                new BootstrapRequest(
                    condominiumName,
                    administratorName,
                    username,
                    password),
                bootstrapToken);

            CondominiumIdText = result.CondominiumId.ToString();
            Username = username;

            await settingsService.SaveAsync(
                new DesktopSettings(
                    ApiBaseUrl.Trim(),
                    result.CondominiumId,
                    username.Trim()));

            SetupStatus = "INICIALIZADO";
            Status = "Configuração inicial concluída. Faça login com o administrador criado.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await realtimeService.DisconnectAsync();
        sessionState.Clear();
        AuthenticationStatus = "DESCONECTADO";
        Status = "Sessão encerrada.";
    }
}

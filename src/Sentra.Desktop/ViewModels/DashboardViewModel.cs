using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class DashboardViewModel(ISentraApiClient apiClient)
    : PageViewModel("Central operacional")
{
    [ObservableProperty]
    private string _serverStatus = "VERIFICANDO";

    [ObservableProperty]
    private string _sessionStatus = "AGUARDANDO CONFIGURAÇÃO";

    [ObservableProperty]
    private string _whatsAppStatus = "M2 — NÃO CONFIGURADO";

    [ObservableProperty]
    private string _intelligenceStatus = "M3 — NÃO CONFIGURADO";

    [ObservableProperty]
    private string _operationalStatus = "Carregue os dados para iniciar.";

    [ObservableProperty]
    private int? _blocksCount;

    [ObservableProperty]
    private int? _unitsCount;

    [ObservableProperty]
    private int? _residentsCount;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            ServerStatus = await apiClient.IsServerAliveAsync()
                ? "ONLINE"
                : "INDISPONÍVEL";

            if (ServerStatus != "ONLINE")
            {
                OperationalStatus = "Servidor não respondeu ao health check.";
                return;
            }

            var setup = await apiClient.GetSetupStatusAsync();
            if (!setup.IsInitialized)
            {
                SessionStatus = "CONFIGURAÇÃO INICIAL NECESSÁRIA";
                OperationalStatus = "Abra Configurações para concluir o primeiro cadastro.";
                return;
            }

            try
            {
                var blocks = await apiClient.GetBlocksAsync(pageSize: 1);
                var units = await apiClient.GetUnitsAsync(pageSize: 1);
                var residents = await apiClient.GetResidentsAsync(pageSize: 1);

                BlocksCount = blocks.Total;
                UnitsCount = units.Total;
                ResidentsCount = residents.Total;
                SessionStatus = "AUTENTICADO";
                OperationalStatus = "Dados operacionais sincronizados com o servidor.";
            }
            catch (SentraApiException exception) when (
                exception.StatusCode is System.Net.HttpStatusCode.Unauthorized
                    or System.Net.HttpStatusCode.Forbidden)
            {
                SessionStatus = "AGUARDANDO AUTENTICAÇÃO";
                OperationalStatus = exception.Message;
            }
        }
        catch (Exception exception)
        {
            ServerStatus = "ERRO";
            OperationalStatus = DescribeError(exception);
        }
    }
}

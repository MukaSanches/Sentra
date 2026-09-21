using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.WhatsApp;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class WhatsAppSettingsViewModel(
    ISentraApiClient apiClient,
    IDesktopSettingsService settingsService)
    : PageViewModel("WhatsApp")
{
    public ObservableCollection<WhatsAppTemplateResponse> Templates { get; } = [];
    public ObservableCollection<WhatsAppFlowResponse> Flows { get; } = [];

    [ObservableProperty]
    private string _state = "AGUARDANDO CONFIGURAÇÃO";

    [ObservableProperty]
    private string? _phoneNumberId;

    [ObservableProperty]
    private string? _displayName;

    [ObservableProperty]
    private string? _displayPhoneNumber;

    [ObservableProperty]
    private string? _qualityRating;

    [ObservableProperty]
    private string _webhookUrl = string.Empty;

    [ObservableProperty]
    private string _configurationStatus = "NÃO VERIFICADO";

    [ObservableProperty]
    private string _pendingSettings = "—";

    [ObservableProperty]
    private string _status = "Credenciais Meta ficam somente no servidor.";

    public async Task LoadAsync()
    {
        try
        {
            var settings = await settingsService.LoadAsync();
            var baseUri = settings.TryGetApiBaseUri();
            WebhookUrl = baseUri is null
                ? string.Empty
                : new Uri(
                    baseUri,
                    "api/v1/integrations/whatsapp/webhook").ToString();

            var configuration = await apiClient.GetWhatsAppConfigurationAsync();
            ConfigurationStatus = configuration.IsConfigured
                ? "CONFIGURAÇÃO PRESENTE"
                : "AGUARDANDO CONFIGURAÇÃO";
            PendingSettings = configuration.MissingOrInvalidSettings.Count == 0
                ? "Nenhuma pendência estrutural detectada."
                : string.Join(Environment.NewLine, configuration.MissingOrInvalidSettings);

            var current = await apiClient.GetWhatsAppStatusAsync();
            State = current.State;
            PhoneNumberId = current.PhoneNumberId;
            DisplayName = current.DisplayName;

            if (string.Equals(
                    current.State,
                    "Connected",
                    StringComparison.OrdinalIgnoreCase))
            {
                await LoadCatalogAsync();
            }
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task VerifyAsync()
    {
        try
        {
            Status = "Validando número, WABA, webhook subscription, templates e Flows...";

            var result = await apiClient.VerifyWhatsAppAsync();

            State = result.State;
            PhoneNumberId = result.PhoneNumberId;
            DisplayName = result.VerifiedName;
            DisplayPhoneNumber = result.DisplayPhoneNumber;
            QualityRating = result.QualityRating;

            Status =
                $"Validação real concluída: {result.ApprovedTemplateCount} template(s) aprovado(s), " +
                $"{result.PublishedFlowCount} Flow(s) publicado(s).";

            await LoadCatalogAsync();
        }
        catch (Exception exception)
        {
            State = "ERRO DE VALIDAÇÃO";
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task LoadCatalogAsync()
    {
        try
        {
            var templates = await apiClient.GetTemplatesAsync();
            var flows = await apiClient.GetFlowsAsync();

            Templates.Clear();
            foreach (var template in templates)
            {
                Templates.Add(template);
            }

            Flows.Clear();
            foreach (var flow in flows)
            {
                Flows.Add(flow);
            }
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }
}

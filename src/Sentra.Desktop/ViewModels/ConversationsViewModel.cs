using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.WhatsApp;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class ConversationsViewModel : PageViewModel
{
    private readonly ISentraApiClient _apiClient;
    private readonly ISentraRealtimeClient _realtimeClient;

    public ConversationsViewModel(
        ISentraApiClient apiClient,
        ISentraRealtimeClient realtimeClient)
        : base("Conversas")
    {
        _apiClient = apiClient;
        _realtimeClient = realtimeClient;
        _realtimeClient.WhatsAppChanged += OnWhatsAppChanged;
    }

    public ObservableCollection<WhatsAppConversationResponse> Conversations { get; } = [];
    public ObservableCollection<WhatsAppMessageResponse> Messages { get; } = [];
    public ObservableCollection<WhatsAppTemplateResponse> Templates { get; } = [];

    [ObservableProperty]
    private WhatsAppConversationResponse? _selectedConversation;

    [ObservableProperty]
    private WhatsAppTemplateResponse? _selectedTemplate;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private string _templateParameters = string.Empty;

    [ObservableProperty]
    private string _status = "WhatsApp ainda não carregado.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            await _realtimeClient.ConnectAsync();
            var conversations = await _apiClient.GetWhatsAppConversationsAsync();
            Conversations.Clear();
            foreach (var conversation in conversations.Items)
                Conversations.Add(conversation);

            var templates = await _apiClient.GetWhatsAppTemplatesAsync();
            Templates.Clear();
            foreach (var template in templates.Where(x =>
                         string.Equals(x.Status, "APPROVED", StringComparison.OrdinalIgnoreCase)))
            {
                Templates.Add(template);
            }

            Status = $"{conversations.Total} conversa(s).";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task LoadMessagesAsync()
    {
        if (SelectedConversation is null)
        {
            Status = "Selecione uma conversa.";
            return;
        }

        try
        {
            var messages = await _apiClient.GetWhatsAppMessagesAsync(SelectedConversation.Id);
            Messages.Clear();
            foreach (var message in messages.Items.OrderBy(x => x.MessageTimestamp))
                Messages.Add(message);

            Status = SelectedConversation.CustomerServiceWindowOpen
                ? "Janela de atendimento aberta."
                : "Janela de 24 horas fechada: use um template aprovado.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task SendTextAsync()
    {
        if (SelectedConversation is null || string.IsNullOrWhiteSpace(MessageText))
        {
            Status = "Selecione uma conversa e informe a mensagem.";
            return;
        }

        try
        {
            await _apiClient.SendWhatsAppTextAsync(new SendWhatsAppTextRequest(
                SelectedConversation.ExternalParticipantId,
                MessageText.Trim()));
            MessageText = string.Empty;
            await LoadMessagesAsync();
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task SendTemplateAsync()
    {
        if (SelectedConversation is null || SelectedTemplate is null)
        {
            Status = "Selecione a conversa e o template.";
            return;
        }

        try
        {
            var parameters = string.IsNullOrWhiteSpace(TemplateParameters)
                ? null
                : TemplateParameters.Split('|', StringSplitOptions.TrimEntries);

            await _apiClient.SendWhatsAppTemplateAsync(new SendWhatsAppTemplateRequest(
                SelectedConversation.ExternalParticipantId,
                SelectedTemplate.Name,
                SelectedTemplate.Language,
                parameters));
            TemplateParameters = string.Empty;
            await LoadMessagesAsync();
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    partial void OnSelectedConversationChanged(WhatsAppConversationResponse? value)
    {
        if (value is not null)
            _ = LoadMessagesAsync();
    }

    private void OnWhatsAppChanged(object? sender, EventArgs e)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;

        _ = dispatcher.InvokeAsync(async () =>
        {
            await LoadAsync();
            if (SelectedConversation is not null)
                await LoadMessagesAsync();
        });
    }
}

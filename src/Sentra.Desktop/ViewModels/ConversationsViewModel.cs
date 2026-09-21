using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.WhatsApp;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class ConversationsViewModel
    : PageViewModel
{
    private readonly ISentraApiClient _apiClient;
    private readonly RealtimeService _realtimeService;

    public ConversationsViewModel(
        ISentraApiClient apiClient,
        RealtimeService realtimeService)
        : base("Conversas")
    {
        _apiClient = apiClient;
        _realtimeService = realtimeService;
        _realtimeService.ConversationChanged += OnConversationChanged;
    }

    public ObservableCollection<ConversationSummaryResponse> Conversations { get; } = [];
    public ObservableCollection<ConversationMessageResponse> Messages { get; } = [];
    public ObservableCollection<ConversationAttachmentResponse> Attachments { get; } = [];
    public ObservableCollection<WhatsAppTemplateResponse> Templates { get; } = [];
    public ObservableCollection<WhatsAppFlowResponse> Flows { get; } = [];

    [ObservableProperty]
    private ConversationSummaryResponse? _selectedConversation;

    [ObservableProperty]
    private ConversationMessageResponse? _selectedMessage;

    [ObservableProperty]
    private WhatsAppTemplateResponse? _selectedTemplate;

    [ObservableProperty]
    private WhatsAppFlowResponse? _selectedFlow;

    [ObservableProperty]
    private string _draftText = string.Empty;

    [ObservableProperty]
    private string _templateParameters = string.Empty;

    [ObservableProperty]
    private string _flowBody = "Preencha os dados solicitados.";

    [ObservableProperty]
    private string _flowCallToAction = "Abrir formulário";

    [ObservableProperty]
    private string? _flowScreen;

    [ObservableProperty]
    private string _interactiveBody = "Confirma esta solicitação?";

    [ObservableProperty]
    private IntelligenceAnalysisResponse? _intelligenceAnalysis;

    [ObservableProperty]
    private Guid? _pendingActionId;

    [ObservableProperty]
    private string _status = "Selecione uma conversa.";

    partial void OnSelectedConversationChanged(
        ConversationSummaryResponse? value)
    {
        if (value is not null)
        {
            _ = LoadSelectedConversationAsync();
        }
    }

    partial void OnSelectedMessageChanged(
        ConversationMessageResponse? value)
    {
        if (value is not null)
        {
            _ = LoadAttachmentsAsync(value);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            var items = await _apiClient.GetConversationsAsync();

            Conversations.Clear();
            foreach (var item in items)
            {
                Conversations.Add(item);
            }

            try
            {
                var templates = await _apiClient.GetTemplatesAsync();
                Templates.Clear();
                foreach (var template in templates.Where(item =>
                             string.Equals(
                                 item.Status,
                                 "APPROVED",
                                 StringComparison.OrdinalIgnoreCase)))
                {
                    Templates.Add(template);
                }

                var flows = await _apiClient.GetFlowsAsync();
                Flows.Clear();
                foreach (var flow in flows.Where(item =>
                             string.Equals(
                                 item.Status,
                                 "PUBLISHED",
                                 StringComparison.OrdinalIgnoreCase)))
                {
                    Flows.Add(flow);
                }
            }
            catch (SentraApiException)
            {
                Templates.Clear();
                Flows.Clear();
            }

            Status = $"{Conversations.Count} conversa(s) carregada(s).";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task AnalyzeIntelligenceAsync()
    {
        if (SelectedConversation is null)
        {
            Status = "Selecione uma conversa.";
            return;
        }

        try
        {
            IntelligenceAnalysis = await _apiClient.AnalyzeConversationAsync(SelectedConversation.Id);
            PendingActionId = IntelligenceAnalysis.PendingActionId;
            Status = IntelligenceAnalysis.Summary;
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task ConfirmIntelligenceActionAsync()
    {
        if (PendingActionId is null)
        {
            Status = "Não há ação pendente para confirmar.";
            return;
        }

        try
        {
            await _apiClient.ResolvePendingActionAsync(PendingActionId.Value, "confirm");
            Status = "Ação confirmada pelo porteiro e executada pelo Policy Engine.";
            PendingActionId = null;
            await LoadSelectedConversationAsync();
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task RejectIntelligenceActionAsync()
    {
        if (PendingActionId is null)
        {
            Status = "Não há ação pendente.";
            return;
        }

        try
        {
            await _apiClient.ResolvePendingActionAsync(PendingActionId.Value, "reject");
            Status = "Sugestão rejeitada. Nenhuma ação operacional foi executada.";
            PendingActionId = null;
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task SendTextAsync()
    {
        if (SelectedConversation is null ||
            string.IsNullOrWhiteSpace(DraftText))
        {
            Status = "Selecione uma conversa e escreva a mensagem.";
            return;
        }

        try
        {
            await _apiClient.SendTextAsync(
                SelectedConversation.Id,
                DraftText.Trim());

            DraftText = string.Empty;
            await LoadSelectedConversationAsync();
            Status = "Mensagem aceita pelo backend; acompanhe o status de entrega.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task SendTemplateAsync()
    {
        if (SelectedConversation is null ||
            SelectedTemplate is null)
        {
            Status = "Selecione a conversa e um template aprovado.";
            return;
        }

        try
        {
            var parameters = string.IsNullOrWhiteSpace(TemplateParameters)
                ? Array.Empty<string>()
                : TemplateParameters.Split(
                    '|',
                    StringSplitOptions.TrimEntries |
                    StringSplitOptions.RemoveEmptyEntries);

            await _apiClient.SendTemplateAsync(
                SelectedConversation.Id,
                new SendWhatsAppTemplateRequest(
                    Guid.NewGuid(),
                    SelectedTemplate.Name,
                    SelectedTemplate.Language,
                    parameters));

            await LoadSelectedConversationAsync();
            Status = "Template enviado à Meta para processamento.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task SendConfirmationButtonsAsync()
    {
        if (SelectedConversation is null)
        {
            Status = "Selecione uma conversa.";
            return;
        }

        try
        {
            await _apiClient.SendButtonsAsync(
                SelectedConversation.Id,
                new SendWhatsAppReplyButtonsRequest(
                    Guid.NewGuid(),
                    InteractiveBody,
                    [
                        new WhatsAppReplyButtonRequest("confirm", "Confirmar"),
                        new WhatsAppReplyButtonRequest("deny", "Negar")
                    ]));

            await LoadSelectedConversationAsync();
            Status = "Mensagem interativa enviada.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task SendFlowAsync()
    {
        if (SelectedConversation is null ||
            SelectedFlow is null)
        {
            Status = "Selecione a conversa e um Flow publicado.";
            return;
        }

        try
        {
            await _apiClient.SendFlowAsync(
                SelectedConversation.Id,
                new SendWhatsAppFlowRequest(
                    Guid.NewGuid(),
                    SelectedFlow.Id,
                    FlowCallToAction,
                    FlowBody,
                    string.IsNullOrWhiteSpace(FlowScreen)
                        ? null
                        : FlowScreen.Trim(),
                    null));

            await LoadSelectedConversationAsync();
            Status = "Flow enviado.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    public async Task SendMediaFileAsync(
        string path,
        string kind,
        string? caption)
    {
        if (SelectedConversation is null)
        {
            Status = "Selecione uma conversa.";
            return;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var fileName = Path.GetFileName(path);
            var contentType = GetContentType(path);

            await _apiClient.SendMediaAsync(
                SelectedConversation.Id,
                kind,
                stream,
                fileName,
                contentType,
                caption);

            await LoadSelectedConversationAsync();
            Status = "Mídia enviada à Meta.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    public async Task DownloadAttachmentAsync(
        ConversationAttachmentResponse attachment,
        string destinationPath)
    {
        if (SelectedConversation is null)
        {
            return;
        }

        try
        {
            await using var stream = File.Create(destinationPath);

            await _apiClient.DownloadAttachmentAsync(
                SelectedConversation.Id,
                attachment.Id,
                stream);

            Status = "Anexo salvo.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    private async Task LoadSelectedConversationAsync()
    {
        if (SelectedConversation is null)
        {
            return;
        }

        try
        {
            var messages = await _apiClient.GetMessagesAsync(
                SelectedConversation.Id);

            Messages.Clear();
            foreach (var message in messages)
            {
                Messages.Add(message);
            }

            var lastInbound = messages
                .Where(item =>
                    string.Equals(
                        item.Direction,
                        "Inbound",
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.OccurredAt)
                .FirstOrDefault();

            if (lastInbound is not null &&
                !string.IsNullOrWhiteSpace(lastInbound.ExternalMessageId))
            {
                try
                {
                    await _apiClient.MarkReadAsync(
                        SelectedConversation.Id,
                        lastInbound.Id);
                }
                catch (SentraApiException)
                {
                    // A conversa continua utilizável mesmo se o read receipt falhar.
                }
            }

            Attachments.Clear();
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    private async Task LoadAttachmentsAsync(
        ConversationMessageResponse message)
    {
        if (SelectedConversation is null)
        {
            return;
        }

        try
        {
            var attachments = await _apiClient.GetAttachmentsAsync(
                SelectedConversation.Id,
                message.Id);

            Attachments.Clear();
            foreach (var attachment in attachments)
            {
                Attachments.Add(attachment);
            }
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    private void OnConversationChanged(object? sender, EventArgs e)
    {
        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(
            async () =>
            {
                await LoadAsync();

                if (SelectedConversation is not null)
                {
                    await LoadSelectedConversationAsync();
                }
            });
    }

    private static string GetContentType(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
}

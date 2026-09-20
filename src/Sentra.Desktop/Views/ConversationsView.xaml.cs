using Microsoft.Win32;
using Sentra.Contracts.WhatsApp;
using Sentra.Desktop.ViewModels;

namespace Sentra.Desktop.Views;

public partial class ConversationsView : System.Windows.Controls.UserControl
{
    public static readonly System.Windows.DependencyProperty SelectedAttachmentProperty =
        System.Windows.DependencyProperty.Register(
            nameof(SelectedAttachment),
            typeof(ConversationAttachmentResponse),
            typeof(ConversationsView));

    public ConversationsView()
    {
        InitializeComponent();
    }

    public ConversationAttachmentResponse? SelectedAttachment
    {
        get => (ConversationAttachmentResponse?)GetValue(SelectedAttachmentProperty);
        set => SetValue(SelectedAttachmentProperty, value);
    }

    private Task SendFileAsync(
        string kind,
        string filter)
    {
        if (DataContext is not ConversationsViewModel viewModel)
        {
            return Task.CompletedTask;
        }

        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = false,
            Filter = filter
        };

        return dialog.ShowDialog() == true
            ? viewModel.SendMediaFileAsync(dialog.FileName, kind, caption: null)
            : Task.CompletedTask;
    }

    private async void OnSendImageClick(
        object sender,
        System.Windows.RoutedEventArgs e)
        => await SendFileAsync(
            "Image",
            "Imagens|*.jpg;*.jpeg;*.png;*.webp|Todos os arquivos|*.*");

    private async void OnSendDocumentClick(
        object sender,
        System.Windows.RoutedEventArgs e)
        => await SendFileAsync(
            "Document",
            "Documentos|*.pdf;*.txt;*.doc;*.docx;*.xls;*.xlsx|Todos os arquivos|*.*");

    private async void OnSendAudioClick(
        object sender,
        System.Windows.RoutedEventArgs e)
        => await SendFileAsync(
            "Audio",
            "Áudio|*.mp3;*.ogg;*.wav;*.m4a|Todos os arquivos|*.*");

    private async void OnSendVideoClick(
        object sender,
        System.Windows.RoutedEventArgs e)
        => await SendFileAsync(
            "Video",
            "Vídeo|*.mp4;*.3gp|Todos os arquivos|*.*");

    private async void OnDownloadAttachmentClick(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ConversationsViewModel viewModel ||
            SelectedAttachment is null)
        {
            return;
        }

        var fileName = string.IsNullOrWhiteSpace(SelectedAttachment.FileName)
            ? "whatsapp-anexo"
            : SelectedAttachment.FileName;

        var dialog = new SaveFileDialog
        {
            FileName = fileName,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() == true)
        {
            await viewModel.DownloadAttachmentAsync(
                SelectedAttachment,
                dialog.FileName);
        }
    }
}

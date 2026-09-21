using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sentra.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ConnectionViewModel _connection;
    private readonly ConversationsViewModel _conversations;
    private readonly WhatsAppSettingsViewModel _whatsApp;
    private readonly OperationsViewModel _operations;

    public MainWindowViewModel(
        ConnectionViewModel connection,
        ConversationsViewModel conversations,
        WhatsAppSettingsViewModel whatsApp,
        OperationsViewModel operations)
    {
        _connection = connection;
        _conversations = conversations;
        _whatsApp = whatsApp;
        _operations = operations;
        _currentPage = connection;

        _connection.Authenticated += OnAuthenticated;
    }

    [ObservableProperty]
    private PageViewModel _currentPage;

    public string CurrentTitle => CurrentPage.Title;

    partial void OnCurrentPageChanged(PageViewModel value)
        => OnPropertyChanged(nameof(CurrentTitle));

    [RelayCommand]
    public Task InitializeAsync()
        => _connection.InitializeAsync();

    [RelayCommand]
    private Task ShowConnectionAsync()
    {
        CurrentPage = _connection;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ShowConversationsAsync()
    {
        CurrentPage = _conversations;
        await _conversations.LoadAsync();
    }

    [RelayCommand]
    private async Task ShowWhatsAppAsync()
    {
        CurrentPage = _whatsApp;
        await _whatsApp.LoadAsync();
    }

    private async void OnAuthenticated(object? sender, EventArgs e)
    {
        CurrentPage = _conversations;
        await _conversations.LoadAsync();
    }
}

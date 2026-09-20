using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sentra.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly DashboardViewModel _dashboard;
    private readonly BlocksViewModel _blocks;
    private readonly UnitsViewModel _units;
    private readonly ResidentsViewModel _residents;
    private readonly UsersPermissionsViewModel _usersPermissions;
    private readonly ConfigurationViewModel _configuration;

    public MainWindowViewModel(
        DashboardViewModel dashboard,
        BlocksViewModel blocks,
        UnitsViewModel units,
        ResidentsViewModel residents,
        UsersPermissionsViewModel usersPermissions,
        ConfigurationViewModel configuration)
    {
        _dashboard = dashboard;
        _blocks = blocks;
        _units = units;
        _residents = residents;
        _usersPermissions = usersPermissions;
        _configuration = configuration;
        _currentPage = dashboard;
    }

    [ObservableProperty]
    private PageViewModel _currentPage;

    public string CurrentTitle => CurrentPage.Title;

    partial void OnCurrentPageChanged(PageViewModel value)
        => OnPropertyChanged(nameof(CurrentTitle));

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await _configuration.LoadAsync();
        await _dashboard.RefreshAsync();
    }

    [RelayCommand]
    private async Task ShowDashboardAsync()
    {
        CurrentPage = _dashboard;
        await _dashboard.RefreshAsync();
    }

    [RelayCommand]
    private async Task ShowBlocksAsync()
    {
        CurrentPage = _blocks;
        await _blocks.LoadAsync();
    }

    [RelayCommand]
    private async Task ShowUnitsAsync()
    {
        CurrentPage = _units;
        await _units.LoadAsync();
    }

    [RelayCommand]
    private async Task ShowResidentsAsync()
    {
        CurrentPage = _residents;
        await _residents.LoadAsync();
    }

    [RelayCommand]
    private async Task ShowUsersPermissionsAsync()
    {
        CurrentPage = _usersPermissions;
        await _usersPermissions.LoadAsync();
    }

    [RelayCommand]
    private async Task ShowConfigurationAsync()
    {
        CurrentPage = _configuration;
        await _configuration.LoadAsync();
    }
}

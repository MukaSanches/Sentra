using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentra.Desktop.Services;
using Sentra.Desktop.ViewModels;

namespace Sentra.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddHttpClient("SENTRA", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                });

                services.AddSingleton<IDesktopSettingsService, JsonDesktopSettingsService>();
                services.AddSingleton<IAccessTokenProvider, EnvironmentAccessTokenProvider>();
                services.AddSingleton<ISentraApiClient, SentraApiClient>();
                services.AddSingleton<ISentraRealtimeClient, SentraRealtimeClient>();

                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<BlocksViewModel>();
                services.AddSingleton<UnitsViewModel>();
                services.AddSingleton<ResidentsViewModel>();
                services.AddSingleton<ConversationsViewModel>();
                services.AddSingleton<UsersPermissionsViewModel>();
                services.AddSingleton<ConfigurationViewModel>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override async void OnExit(System.Windows.ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

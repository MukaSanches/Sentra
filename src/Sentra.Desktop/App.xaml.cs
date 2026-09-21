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
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                services.AddSingleton<IDesktopSettingsService, DesktopSettingsService>();
                services.AddSingleton<SessionState>();
                services.AddSingleton<ISentraApiClient, SentraApiClient>();
                services.AddSingleton<RealtimeService>();\n                services.AddSingleton<IOfflineCacheService, OfflineCacheService>();\n                services.AddHostedService<OfflineSyncService>();

                services.AddSingleton<ConnectionViewModel>();
                services.AddSingleton<ConversationsViewModel>();
                services.AddSingleton<WhatsAppSettingsViewModel>();\n                services.AddSingleton<OperationsViewModel>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override async void OnExit(System.Windows.ExitEventArgs e)
    {
        if (_host is not null)
        {
            var realtime = _host.Services.GetService<RealtimeService>();
            if (realtime is not null)
            {
                await realtime.DisposeAsync();
            }

            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

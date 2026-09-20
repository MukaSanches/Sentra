using CommunityToolkit.Mvvm.ComponentModel;

namespace Sentra.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public string ServerStatus => "AGUARDANDO CONFIGURAÇÃO";
    public string WhatsAppStatus => "AGUARDANDO CONFIGURAÇÃO";
    public string IntelligenceStatus => "AGUARDANDO CONFIGURAÇÃO";
}

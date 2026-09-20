using Sentra.Desktop.ViewModels;

namespace Sentra.Desktop;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}

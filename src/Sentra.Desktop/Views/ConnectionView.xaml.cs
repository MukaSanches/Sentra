using Sentra.Desktop.ViewModels;

namespace Sentra.Desktop.Views;

public partial class ConnectionView : System.Windows.Controls.UserControl
{
    public ConnectionView()
    {
        InitializeComponent();
    }

    private async void OnLoginClick(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ConnectionViewModel viewModel)
        {
            await viewModel.LoginAsync(LoginPassword.Password);
            LoginPassword.Clear();
        }
    }

    private async void OnBootstrapClick(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ConnectionViewModel viewModel)
        {
            return;
        }

        await viewModel.BootstrapAsync(
            BootstrapCondominiumName.Text,
            BootstrapAdministratorName.Text,
            BootstrapUsername.Text,
            BootstrapPassword.Password,
            BootstrapToken.Password);

        BootstrapPassword.Clear();
        BootstrapToken.Clear();
    }
}

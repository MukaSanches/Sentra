using Sentra.Desktop.ViewModels;

namespace Sentra.Desktop.Views;

public partial class AdministrationView : System.Windows.Controls.UserControl
{
    public AdministrationView()
    {
        InitializeComponent();
    }

    private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AdministrationViewModel vm &&
            sender is System.Windows.Controls.PasswordBox box)
        {
            vm.EmployeePassword = box.Password;
        }
    }
}

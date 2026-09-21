using Sentra.Desktop.ViewModels;

namespace Sentra.Desktop.Views;

public partial class AdministrationView : System.Windows.Controls.UserControl
{
    public AdministrationView()
    {
        InitializeComponent();
    }

    private async void OnImportUnitsClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = CreateImportDialog();
        if (dialog.ShowDialog() == true &&
            DataContext is AdministrationViewModel vm)
        {
            await vm.ImportUnitsFileAsync(dialog.FileName);
        }
    }

    private async void OnImportResidentsClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = CreateImportDialog();
        if (dialog.ShowDialog() == true &&
            DataContext is AdministrationViewModel vm)
        {
            await vm.ImportResidentsFileAsync(dialog.FileName);
        }
    }

    private static Microsoft.Win32.OpenFileDialog CreateImportDialog()
        => new()
        {
            Title = "Importar dados no SENTRA",
            Filter = "CSV ou Excel (*.csv;*.xlsx)|*.csv;*.xlsx",
            CheckFileExists = true,
            Multiselect = false
        };

    private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AdministrationViewModel vm &&
            sender is System.Windows.Controls.PasswordBox box)
        {
            vm.EmployeePassword = box.Password;
        }
    }
}

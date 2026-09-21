using Sentra.Desktop.ViewModels;

namespace Sentra.Desktop.Views;

public partial class AdministrationView : System.Windows.Controls.UserControl
{
    public AdministrationView()
    {
        InitializeComponent();
    }

    private void OnBackupPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AdministrationViewModel vm &&
            sender is System.Windows.Controls.PasswordBox box)
        {
            vm.BackupPassphrase = box.Password;
        }
    }

    private async void OnExportBackupClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Salvar backup SENTRA",
            Filter = "Backup SENTRA (*.sentra)|*.sentra",
            FileName = $"SENTRA-backup-{DateTime.Now:yyyyMMdd-HHmmss}.sentra"
        };

        if (dialog.ShowDialog() == true &&
            DataContext is AdministrationViewModel vm)
        {
            await vm.ExportBackupFileAsync(dialog.FileName);
        }
    }

    private async void OnRestoreBackupClick(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Restaurar backup SENTRA",
            Filter = "Backup SENTRA (*.sentra)|*.sentra",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true &&
            DataContext is AdministrationViewModel vm)
        {
            var result = System.Windows.MessageBox.Show(
                "A restauração adicionará registros ausentes ao condomínio atual sem apagar dados existentes. Continuar?",
                "Restaurar backup",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await vm.RestoreBackupFileAsync(dialog.FileName);
            }
        }
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

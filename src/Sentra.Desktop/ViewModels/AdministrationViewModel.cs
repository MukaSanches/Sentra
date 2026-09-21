using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.Admin;
using Sentra.Contracts.Operations;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class AdministrationViewModel(ISentraApiClient api, IUpdateService updates)
    : PageViewModel("Administração")
{
    public ObservableCollection<PermissionAdminResponse> Permissions { get; } = [];
    public ObservableCollection<RoleAdminResponse> Roles { get; } = [];
    public ObservableCollection<EmployeeAdminResponse> Employees { get; } = [];
    public ObservableCollection<AuditRecordResponse> Audit { get; } = [];

    [ObservableProperty] private RoleAdminResponse? _selectedRole;
    [ObservableProperty] private string _roleName = string.Empty;
    [ObservableProperty] private string _roleDescription = string.Empty;
    [ObservableProperty] private string _rolePermissionCodes = string.Empty;

    [ObservableProperty] private string _employeeName = string.Empty;
    [ObservableProperty] private string _employeeUsername = string.Empty;
    [ObservableProperty] private string _employeePassword = string.Empty;
    [ObservableProperty] private UpdateCheckResult? _lastUpdateCheck;
    [ObservableProperty] private string _updateStatus = "Atualização ainda não verificada.";
    [ObservableProperty] private string _status = "Administração pronta.";

    public async Task LoadAsync()
    {
        try
        {
            Replace(Permissions, await api.GetAdminPermissionsAsync());
            Replace(Roles, await api.GetAdminRolesAsync());
            Replace(Employees, await api.GetAdminEmployeesAsync());
            Replace(Audit, await api.GetAuditAsync(300));
            Status = $"{Employees.Count} usuário(s), {Roles.Count} perfil(is).";
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        LastUpdateCheck = await updates.CheckAsync();
        UpdateStatus = LastUpdateCheck.Message;
    }

    [RelayCommand]
    private void OpenUpdate()
    {
        if (LastUpdateCheck is not null) updates.OpenRelease(LastUpdateCheck);
    }

    [RelayCommand]
    private async Task CreateRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(RoleName))
        {
            Status = "Informe o nome do perfil.";
            return;
        }

        var codes = RolePermissionCodes
            .Split(new[] { ',', ';', '|', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);


        try
        {
            await api.CreateAdminRoleAsync(new CreateRoleAdminRequest(
                RoleName.Trim(),
                string.IsNullOrWhiteSpace(RoleDescription) ? null : RoleDescription.Trim(),
                codes));
            RoleName = RoleDescription = RolePermissionCodes = string.Empty;
            await LoadAsync();
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task CreateEmployeeAsync()
    {
        if (SelectedRole is null)
        {
            Status = "Selecione o perfil do usuário.";
            return;
        }

        try
        {
            await api.CreateAdminEmployeeAsync(new CreateEmployeeAdminRequest(
                EmployeeName.Trim(),
                EmployeeUsername.Trim(),
                EmployeePassword,
                SelectedRole.Id));
            EmployeeName = EmployeeUsername = EmployeePassword = string.Empty;
            await LoadAsync();
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }
}

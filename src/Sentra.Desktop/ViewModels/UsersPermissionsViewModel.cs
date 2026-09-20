using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.Operations;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class UsersPermissionsViewModel(ISentraApiClient apiClient)
    : PageViewModel("Usuários e permissões")
{
    public ObservableCollection<EmployeeResponse> Employees { get; } = [];
    public ObservableCollection<RoleResponse> Roles { get; } = [];
    public ObservableCollection<PermissionOptionViewModel> Permissions { get; } =
    [
        new("condominium.read", "Consultar condomínio"),
        new("condominium.write", "Alterar condomínio"),
        new("residents.read", "Consultar moradores"),
        new("residents.write", "Gerenciar moradores"),
        new("employees.read", "Consultar funcionários"),
        new("employees.manage", "Gerenciar funcionários"),
        new("roles.manage", "Gerenciar papéis e permissões")
    ];

    [ObservableProperty]
    private EmployeeResponse? _selectedEmployee;

    [ObservableProperty]
    private RoleResponse? _selectedRole;

    [ObservableProperty]
    private string _newRoleName = string.Empty;

    [ObservableProperty]
    private string _status = "Nenhum dado carregado.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var employees = await apiClient.GetEmployeesAsync(pageSize: 100);
            var roles = await apiClient.GetRolesAsync();

            Employees.Clear();
            foreach (var employee in employees.Items)
            {
                Employees.Add(employee);
            }

            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
            }

            Status = $"{employees.Total} funcionário(s), {roles.Count} papel(is).";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task CreateRoleAsync()
    {
        try
        {
            var created = await apiClient.CreateRoleAsync(new CreateRoleRequest(NewRoleName));
            Roles.Add(created);
            NewRoleName = string.Empty;
            Status = "Papel criado.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task ApplyPermissionsAsync()
    {
        if (SelectedRole is null)
        {
            Status = "Selecione um papel.";
            return;
        }

        try
        {
            var selected = Permissions.Where(x => x.IsSelected).Select(x => x.Code).ToArray();
            await apiClient.SetRolePermissionsAsync(SelectedRole.Id, selected);
            Status = "Permissões confirmadas pelo servidor.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task AssignRoleAsync()
    {
        if (SelectedEmployee is null || SelectedRole is null)
        {
            Status = "Selecione o funcionário e o papel.";
            return;
        }

        try
        {
            await apiClient.AssignEmployeeRoleAsync(SelectedEmployee.Id, SelectedRole.Id);
            Status = "Papel atribuído ao funcionário.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }
}

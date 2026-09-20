using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.Operations;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class ResidentsViewModel(ISentraApiClient apiClient)
    : PageViewModel("Moradores")
{
    public ObservableCollection<ResidentResponse> Items { get; } = [];

    [ObservableProperty]
    private string? _search;

    [ObservableProperty]
    private string _newFullName = string.Empty;

    [ObservableProperty]
    private string _status = "Nenhum dado carregado.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var response = await apiClient.GetResidentsAsync(Search);
            Items.Clear();
            foreach (var item in response.Items)
            {
                Items.Add(item);
            }

            Status = $"{response.Total} morador(es) encontrado(s).";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        try
        {
            var created = await apiClient.CreateResidentAsync(new CreateResidentRequest(NewFullName));
            Items.Add(created);
            NewFullName = string.Empty;
            Status = "Morador cadastrado e confirmado pelo servidor.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }
}

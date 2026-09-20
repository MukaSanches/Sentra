using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.Operations;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class BlocksViewModel(ISentraApiClient apiClient)
    : PageViewModel("Blocos")
{
    public ObservableCollection<BlockResponse> Items { get; } = [];

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private string? _newCode;

    [ObservableProperty]
    private string _status = "Nenhum dado carregado.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var response = await apiClient.GetBlocksAsync();
            Items.Clear();
            foreach (var item in response.Items)
            {
                Items.Add(item);
            }

            Status = $"{response.Total} bloco(s) encontrado(s).";
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
            var created = await apiClient.CreateBlockAsync(new CreateBlockRequest(NewName, NewCode));
            Items.Add(created);
            NewName = string.Empty;
            NewCode = string.Empty;
            Status = "Bloco cadastrado e confirmado pelo servidor.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }
}

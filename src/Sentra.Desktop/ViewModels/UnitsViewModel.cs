using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.Operations;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class UnitsViewModel(ISentraApiClient apiClient)
    : PageViewModel("Unidades")
{
    public ObservableCollection<UnitResponse> Items { get; } = [];
    public ObservableCollection<BlockResponse> Blocks { get; } = [];

    [ObservableProperty]
    private BlockResponse? _selectedBlock;

    [ObservableProperty]
    private string _newNumber = string.Empty;

    [ObservableProperty]
    private string? _newFloor;

    [ObservableProperty]
    private string _status = "Nenhum dado carregado.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var blocks = await apiClient.GetBlocksAsync(pageSize: 100);
            var units = await apiClient.GetUnitsAsync(pageSize: 100);

            Blocks.Clear();
            foreach (var block in blocks.Items)
            {
                Blocks.Add(block);
            }

            Items.Clear();
            foreach (var unit in units.Items)
            {
                Items.Add(unit);
            }

            Status = $"{units.Total} unidade(s) encontrada(s).";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (SelectedBlock is null)
        {
            Status = "Selecione um bloco.";
            return;
        }

        try
        {
            var created = await apiClient.CreateUnitAsync(
                new CreateUnitRequest(SelectedBlock.Id, NewNumber, NewFloor));
            Items.Add(created);
            NewNumber = string.Empty;
            NewFloor = string.Empty;
            Status = "Unidade cadastrada e confirmada pelo servidor.";
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }
}

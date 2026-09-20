using CommunityToolkit.Mvvm.ComponentModel;

namespace Sentra.Desktop.ViewModels;

public sealed partial class PermissionOptionViewModel(string code, string label) : ObservableObject
{
    public string Code { get; } = code;
    public string Label { get; } = label;

    [ObservableProperty]
    private bool _isSelected;
}

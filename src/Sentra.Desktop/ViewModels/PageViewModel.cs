using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sentra.Desktop.ViewModels;

public abstract class PageViewModel(string title) : ObservableObject
{
    public string Title { get; } = title;

    protected static string DescribeError(Exception exception)
        => exception switch
        {
            Sentra.Desktop.Services.SentraApiException api => api.Message,
            HttpRequestException => "Não foi possível alcançar o servidor SENTRA.",
            OperationCanceledException => "Operação cancelada ou tempo limite atingido.",
            _ => exception.Message
        };
}

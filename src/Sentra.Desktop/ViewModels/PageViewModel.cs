using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public abstract class PageViewModel(string title) : ObservableObject
{
    public string Title { get; } = title;

    protected static string DescribeError(Exception exception)
        => exception switch
        {
            SentraApiException api => api.Message,
            HttpRequestException => "Não foi possível alcançar o servidor SENTRA.",
            OperationCanceledException => "A operação foi cancelada ou excedeu o tempo limite.",
            _ => exception.Message
        };
}

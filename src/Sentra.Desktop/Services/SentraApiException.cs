using System.Net;

namespace Sentra.Desktop.Services;

public sealed class SentraApiException(HttpStatusCode statusCode, string message)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

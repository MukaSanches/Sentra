using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentra.WhatsApp.Meta;

public sealed class MetaWhatsAppApiException : HttpRequestException
{
    public MetaWhatsAppApiException(
        HttpStatusCode statusCode,
        int? graphCode,
        int? graphSubcode,
        string? graphType,
        bool? isTransient,
        string? fbTraceId)
        : base(
            BuildMessage(statusCode, graphCode, graphSubcode),
            inner: null,
            statusCode)
    {
        GraphCode = graphCode;
        GraphSubcode = graphSubcode;
        GraphType = graphType;
        IsTransient = isTransient;
        FbTraceId = fbTraceId;
    }

    public int? GraphCode { get; }
    public int? GraphSubcode { get; }
    public string? GraphType { get; }
    public bool? IsTransient { get; }
    public string? FbTraceId { get; }

    private static string BuildMessage(
        HttpStatusCode statusCode,
        int? graphCode,
        int? graphSubcode)
    {
        var graph = graphCode is null
            ? "sem código Graph"
            : graphSubcode is null
                ? $"Graph {graphCode}"
                : $"Graph {graphCode}/{graphSubcode}";

        return $"Meta WhatsApp API rejeitou a solicitação: HTTP {(int)statusCode}, {graph}.";
    }
}

internal static class MetaGraphHttpResponseExtensions
{
    public static async Task EnsureMetaSuccessAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        int? code = null;
        int? subcode = null;
        string? type = null;
        bool? transient = null;
        string? traceId = null;

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<GraphErrorEnvelope>(
                cancellationToken: cancellationToken);

            code = payload?.Error?.Code;
            subcode = payload?.Error?.ErrorSubcode;
            type = payload?.Error?.Type;
            transient = payload?.Error?.IsTransient;
            traceId = payload?.Error?.FbTraceId;
        }
        catch (Exception exception)
            when (exception is JsonException
                  or NotSupportedException
                  or InvalidOperationException)
        {
            // A resposta de erro pode não ser JSON. Nunca incluímos o corpo bruto
            // na exceção para evitar expor conteúdo ou credenciais por logs.
        }

        throw new MetaWhatsAppApiException(
            response.StatusCode,
            code,
            subcode,
            type,
            transient,
            traceId);
    }

    private sealed record GraphErrorEnvelope(
        [property: JsonPropertyName("error")] GraphError? Error);

    private sealed record GraphError(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("code")] int? Code,
        [property: JsonPropertyName("error_subcode")] int? ErrorSubcode,
        [property: JsonPropertyName("is_transient")] bool? IsTransient,
        [property: JsonPropertyName("fbtrace_id")] string? FbTraceId);
}

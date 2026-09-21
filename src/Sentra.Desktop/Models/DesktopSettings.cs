namespace Sentra.Desktop.Models;

public sealed record DesktopSettings(
    string ApiBaseUrl,
    Guid? CondominiumId,
    string? Username)
{
    public static DesktopSettings Empty { get; } =
        new(string.Empty, null, null);

    public Uri? TryGetApiBaseUri()
    {
        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new Uri(uri.ToString().TrimEnd('/') + "/", UriKind.Absolute);
    }
}

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

        if (uri.Scheme is not (Uri.UriSchemeHttps or Uri.UriSchemeHttp))
        {
            return null;
        }

        return new Uri(uri.ToString().TrimEnd('/') + "/", UriKind.Absolute);
    }
}

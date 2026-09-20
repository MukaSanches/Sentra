namespace Sentra.Desktop.Models;

public sealed record DesktopSettings(string ApiBaseUrl, Guid? CondominiumId)
{
    public static DesktopSettings Empty { get; } = new(string.Empty, null);

    public bool HasServerConfiguration
        => Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

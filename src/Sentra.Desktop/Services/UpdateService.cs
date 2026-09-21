using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace Sentra.Desktop.Services;

public sealed record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseUrl,
    string? InstallerUrl,
    string Message);

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
    void OpenRelease(UpdateCheckResult result);
}

public sealed class UpdateService(IHttpClientFactory httpClientFactory) : IUpdateService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/MukaSanches/Sentra/releases/latest";

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var current = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0);
        var currentText = $"{current.Major}.{current.Minor}.{Math.Max(0, current.Build)}";

        try
        {
            var client = httpClientFactory.CreateClient("SENTRA-UPDATES");
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
            request.Headers.UserAgent.ParseAdd("SENTRA/1.0");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new(false, currentText, null, null, null, "Ainda não existe release publicado.");
            }
            response.EnsureSuccessStatusCode();

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
            var html = root.GetProperty("html_url").GetString();

            string? installer = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name is not null &&
                        name.StartsWith("SENTRA-Setup-", StringComparison.OrdinalIgnoreCase) &&
                        name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        installer = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            var update = Version.TryParse(tag, out var latest) && latest > current;
            return new(
                update,
                currentText,
                tag,
                html,
                installer,
                update ? $"Nova versão {tag} disponível." : "SENTRA está atualizado.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            return new(false, currentText, null, null, null, "Não foi possível verificar atualizações agora.");
        }
    }

    public void OpenRelease(UpdateCheckResult result)
    {
        if (string.IsNullOrWhiteSpace(result.ReleaseUrl))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true });
    }
}

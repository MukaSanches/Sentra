using System.IO;
using System.Text.Json;
using Sentra.Desktop.Models;

namespace Sentra.Desktop.Services;

public interface IDesktopSettingsService
{
    Task<DesktopSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(DesktopSettings settings, CancellationToken cancellationToken = default);
}

public sealed class DesktopSettingsService : IDesktopSettingsService
{
    private readonly string _path;

    public DesktopSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SENTRA",
            "settings.json"))
    {
    }

    public DesktopSettingsService(string path)
    {
        _path = path;
    }

    public async Task<DesktopSettings> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return DesktopSettings.Empty;
        }

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<DesktopSettings>(
            stream,
            cancellationToken: cancellationToken)
            ?? DesktopSettings.Empty;
    }

    public async Task SaveAsync(
        DesktopSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.TryGetApiBaseUri() is null)
        {
            throw new ArgumentException(
                "A URL do servidor precisa ser HTTP ou HTTPS válida.",
                nameof(settings));
        }

        var directory = Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException(
                "Não foi possível determinar o diretório de configuração.");

        Directory.CreateDirectory(directory);

        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(
            stream,
            settings,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken);
    }
}

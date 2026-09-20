using System.IO;
using System.Text.Json;
using Sentra.Desktop.Models;

namespace Sentra.Desktop.Services;

public sealed class JsonDesktopSettingsService
    : IDesktopSettingsService
{
    private readonly string _settingsPath;

    public JsonDesktopSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SENTRA",
            "settings.json"))
    {
    }

    public JsonDesktopSettingsService(string settingsPath)
    {
        _settingsPath = string.IsNullOrWhiteSpace(settingsPath)
            ? throw new ArgumentException("Settings path is required.", nameof(settingsPath))
            : settingsPath;
    }

    public async Task<DesktopSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            return DesktopSettings.Empty;
        }

        await using var stream = File.OpenRead(_settingsPath);
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

        if (!settings.HasServerConfiguration)
        {
            throw new ArgumentException("A URL da API precisa ser HTTP ou HTTPS válida.", nameof(settings));
        }

        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("Não foi possível determinar o diretório de configuração.");
        Directory.CreateDirectory(directory);

        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(
            stream,
            settings,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken);
    }
}

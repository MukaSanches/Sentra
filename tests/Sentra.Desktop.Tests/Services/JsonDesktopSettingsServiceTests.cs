using System.IO;
using Sentra.Desktop.Models;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.Tests.Services;

public sealed class JsonDesktopSettingsServiceTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsNonSecretConfiguration()
    {
        var directory = Path.Combine(Path.GetTempPath(), "sentra-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        try
        {
            var service = new JsonDesktopSettingsService(path);
            var expected = new DesktopSettings("https://sentra.example.test", Guid.NewGuid());

            await service.SaveAsync(expected);
            var actual = await service.LoadAsync();

            Assert.Equal(expected, actual);
            var json = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Save_RejectsNonHttpConfiguration()
    {
        var path = Path.Combine(Path.GetTempPath(), "sentra-tests", Guid.NewGuid().ToString("N"), "settings.json");
        var service = new JsonDesktopSettingsService(path);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(new DesktopSettings("file:///tmp/sentra", Guid.NewGuid())));
    }
}

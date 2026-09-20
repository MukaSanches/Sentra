using System.IO;
using Sentra.Desktop.Models;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.Tests.Services;

public sealed class DesktopSettingsServiceTests
{
    [Fact]
    public async Task SaveAndLoad_PersistsOnlyNonSecretConnectionSettings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = Path.Combine(
            Path.GetTempPath(),
            "sentra-desktop-tests",
            Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");

        try
        {
            var service = new DesktopSettingsService(path);
            var expected = new DesktopSettings(
                "https://sentra.example.test",
                Guid.NewGuid(),
                "porteiro");

            await service.SaveAsync(expected, cancellationToken);
            var actual = await service.LoadAsync(cancellationToken);

            Assert.Equal(expected, actual);

            var json = await File.ReadAllTextAsync(
                path,
                cancellationToken);

            Assert.DoesNotContain(
                "password",
                json,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "accessToken",
                json,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "META_",
                json,
                StringComparison.OrdinalIgnoreCase);
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
    public async Task Save_RejectsNonHttpServer()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "sentra-desktop-tests",
            Guid.NewGuid().ToString("N"),
            "settings.json");

        var service = new DesktopSettingsService(path);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(
                new DesktopSettings(
                    "file:///c:/sentra",
                    Guid.NewGuid(),
                    "porteiro"),
                TestContext.Current.CancellationToken));
    }
}

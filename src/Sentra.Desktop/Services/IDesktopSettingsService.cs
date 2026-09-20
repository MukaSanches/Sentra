using Sentra.Desktop.Models;

namespace Sentra.Desktop.Services;

public interface IDesktopSettingsService
{
    Task<DesktopSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(DesktopSettings settings, CancellationToken cancellationToken = default);
}

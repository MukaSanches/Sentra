namespace Sentra.Desktop.Services;

public sealed class EnvironmentAccessTokenProvider : IAccessTokenProvider
{
    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var value = Environment.GetEnvironmentVariable("SENTRA_ACCESS_TOKEN");
        return ValueTask.FromResult(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }
}

namespace Sentra.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

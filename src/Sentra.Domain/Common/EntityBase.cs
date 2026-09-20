namespace Sentra.Domain.Common;

public abstract class EntityBase
{
    protected EntityBase(DateTimeOffset? createdAt = null)
    {
        Id = Guid.NewGuid();
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    public void MarkUpdated(DateTimeOffset timestamp)
    {
        if (timestamp < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(timestamp), "UpdatedAt cannot be earlier than CreatedAt.");
        }

        UpdatedAt = timestamp;
    }
}

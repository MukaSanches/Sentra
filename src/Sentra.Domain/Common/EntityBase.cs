namespace Sentra.Domain.Common;

public abstract class EntityBase
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; protected init; }

    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

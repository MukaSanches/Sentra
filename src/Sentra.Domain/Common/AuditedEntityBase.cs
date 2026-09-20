namespace Sentra.Domain.Common;

public abstract class AuditedEntityBase : EntityBase
{
    protected AuditedEntityBase()
    {
    }

    protected AuditedEntityBase(string createdBy, DateTimeOffset? createdAt = null)
        : base(createdAt)
    {
        CreatedBy = Guard.Required(createdBy, nameof(createdBy), 160);
        UpdatedBy = CreatedBy;
    }

    public string CreatedBy { get; protected set; } = string.Empty;

    public string UpdatedBy { get; protected set; } = string.Empty;

    protected void Touch(string updatedBy, DateTimeOffset timestamp)
    {
        UpdatedBy = Guard.Required(updatedBy, nameof(updatedBy), 160);
        MarkUpdated(timestamp);
    }
}

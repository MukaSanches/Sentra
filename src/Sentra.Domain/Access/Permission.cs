using Sentra.Domain.Common;

namespace Sentra.Domain.Access;

public sealed class Permission : EntityBase
{
    private Permission()
    {
    }

    public Permission(string code, string description, DateTimeOffset? createdAt = null)
        : base(createdAt)
    {
        Code = Guard.Required(code, nameof(code), 120);
        Description = Guard.Required(description, nameof(description), 200);

        if (!PermissionCodes.All.Contains(Code))
        {
            throw new ArgumentException("Unknown permission code.", nameof(code));
        }
    }

    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;
}

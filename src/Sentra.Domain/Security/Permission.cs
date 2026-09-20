using Sentra.Domain.Common;

namespace Sentra.Domain.Security;

public sealed class Permission : EntityBase
{
    private Permission() { }

    public Permission(string code, string description)
    {
        Code = DomainText.Required(code, nameof(code), 96);
        Description = DomainText.Required(description, nameof(description), 256);
    }

    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
}

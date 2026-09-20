using Sentra.Domain.Common;

namespace Sentra.Domain.Security;

public sealed class Role : EntityBase
{
    private Role() { }

    public Role(Guid condominiumId, string name, string? description = null)
    {
        if (condominiumId == Guid.Empty) throw new ArgumentException("Condomínio inválido.", nameof(condominiumId));

        CondominiumId = condominiumId;
        Name = DomainText.Required(name, nameof(name), 64);
        Description = DomainText.Optional(description, 256);
    }

    public Guid CondominiumId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
}

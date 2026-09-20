using Sentra.Domain.Common;

namespace Sentra.Domain.Condominiums;

public sealed class Block : EntityBase
{
    private Block()
    {
    }

    public Block(Guid condominiumId, string name)
    {
        if (condominiumId == Guid.Empty)
        {
            throw new ArgumentException("Condomínio inválido.", nameof(condominiumId));
        }

        CondominiumId = condominiumId;
        Name = DomainText.Required(name, nameof(name), 80);
    }

    public Guid CondominiumId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}

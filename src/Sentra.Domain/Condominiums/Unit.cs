using Sentra.Domain.Common;

namespace Sentra.Domain.Condominiums;

public sealed class Unit : EntityBase
{
    private Unit() { }

    public Unit(Guid condominiumId, string identifier, string displayName, Guid? blockId = null)
    {
        if (condominiumId == Guid.Empty) throw new ArgumentException("Condomínio inválido.", nameof(condominiumId));
        if (blockId == Guid.Empty) throw new ArgumentException("Bloco inválido.", nameof(blockId));

        CondominiumId = condominiumId;
        BlockId = blockId;
        Identifier = DomainText.Required(identifier, nameof(identifier), 64);
        DisplayName = DomainText.Required(displayName, nameof(displayName), 96);
    }

    public Guid CondominiumId { get; private set; }
    public Guid? BlockId { get; private set; }
    public string Identifier { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}

using Sentra.Domain.Common;

namespace Sentra.Domain.Condominiums;

public sealed class Condominium : EntityBase
{
    private Condominium() { }

    public Condominium(string name)
    {
        Name = DomainText.Required(name, nameof(name), 160);
    }

    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}

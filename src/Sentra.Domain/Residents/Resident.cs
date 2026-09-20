using Sentra.Domain.Common;

namespace Sentra.Domain.Residents;

public sealed class Resident : EntityBase
{
    private Resident()
    {
    }

    public Resident(string fullName)
    {
        FullName = DomainText.Required(fullName, nameof(fullName), 160);
    }

    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}

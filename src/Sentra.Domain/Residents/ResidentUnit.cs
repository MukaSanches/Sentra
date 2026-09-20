using Sentra.Domain.Common;

namespace Sentra.Domain.Residents;

public enum ResidentUnitRole
{
    Owner = 0,
    Tenant = 1,
    Family = 2,
    Other = 3
}

public sealed class ResidentUnit : EntityBase
{
    private ResidentUnit()
    {
    }

    public ResidentUnit(
        Guid residentId,
        Guid unitId,
        ResidentUnitRole role,
        DateTimeOffset startsAt,
        bool isPrimary = true)
        : base(startsAt)
    {
        if (residentId == Guid.Empty)
        {
            throw new ArgumentException("Morador inválido.", nameof(residentId));
        }

        if (unitId == Guid.Empty)
        {
            throw new ArgumentException("Unidade inválida.", nameof(unitId));
        }

        ResidentId = residentId;
        UnitId = unitId;
        Role = role;
        IsPrimary = isPrimary;
        StartsAt = startsAt;
    }

    public Guid ResidentId { get; private set; }
    public Guid UnitId { get; private set; }
    public ResidentUnitRole Role { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }
}

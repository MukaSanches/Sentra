using Sentra.Domain.Common;

namespace Sentra.Domain.Residents;

public sealed class ResidentUnit : AuditedEntityBase
{
    private ResidentUnit()
    {
    }

    public ResidentUnit(
        Guid condominiumId,
        Guid residentId,
        Guid unitId,
        ResidentUnitRole role,
        bool isPrimary,
        string createdBy,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt = null)
        : base(createdBy, startsAt)
    {
        CondominiumId = Guard.RequiredId(condominiumId, nameof(condominiumId));
        ResidentId = Guard.RequiredId(residentId, nameof(residentId));
        UnitId = Guard.RequiredId(unitId, nameof(unitId));

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        if (endsAt.HasValue && endsAt.Value <= startsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "End date must be later than start date.");
        }

        Role = role;
        IsPrimary = isPrimary;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public Guid CondominiumId { get; private set; }

    public Guid ResidentId { get; private set; }

    public Guid UnitId { get; private set; }

    public ResidentUnitRole Role { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset? EndsAt { get; private set; }

    public bool IsActiveAt(DateTimeOffset timestamp)
        => StartsAt <= timestamp && (!EndsAt.HasValue || EndsAt.Value > timestamp);

    public void End(DateTimeOffset endsAt, string updatedBy)
    {
        if (endsAt <= StartsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "End date must be later than start date.");
        }

        EndsAt = endsAt;
        Touch(updatedBy, endsAt);
    }
}

using Sentra.Domain.Residents;
using Sentra.Domain.Security;

namespace Sentra.Domain.Tests.Core;

public sealed class CoreDomainTests
{
    [Fact]
    public void ResidentPhone_NormalizesFormattingWithoutGuessingCountry()
    {
        var normalized =
            ResidentPhone.NormalizeE164("+55 (11) 99999-9999");

        Assert.Equal("+5511999999999", normalized);
    }

    [Fact]
    public void ResidentPhone_RejectsPhoneWithoutCountryPrefix()
    {
        Assert.Throws<ArgumentException>(
            () => ResidentPhone.NormalizeE164("11999999999"));
    }

    [Fact]
    public void Employee_LocksAfterFiveFailedAttempts()
    {
        var now = DateTimeOffset.UtcNow;
        var employee = new Employee(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Porteiro Teste",
            "porteiro",
            "hash-de-teste");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            employee.RegisterFailedLogin(now.AddSeconds(attempt));
        }

        Assert.True(employee.IsLocked(now.AddMinutes(1)));
    }

    [Fact]
    public void ResidentUnit_RejectsEmptyUnit()
    {
        Assert.Throws<ArgumentException>(
            () => new ResidentUnit(
                Guid.NewGuid(),
                Guid.Empty,
                ResidentUnitRole.Owner,
                DateTimeOffset.UtcNow));
    }
}

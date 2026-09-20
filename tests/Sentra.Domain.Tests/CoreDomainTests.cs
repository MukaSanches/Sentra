using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sentra.Domain.Residents;
using Sentra.Domain.Security;

namespace Sentra.Domain.Tests;

[TestClass]
public sealed class CoreDomainTests
{
    [TestMethod]
    public void ResidentPhone_NormalizesFormattingWithoutGuessingCountry()
    {
        var normalized = ResidentPhone.NormalizeE164("+55 (11) 99999-9999");
        Assert.AreEqual("+5511999999999", normalized);
    }

    [TestMethod]
    public void ResidentPhone_RejectsPhoneWithoutCountryPrefix()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ResidentPhone.NormalizeE164("11999999999"));
    }

    [TestMethod]
    public void Employee_LocksAfterFiveFailedAttempts()
    {
        var now = DateTimeOffset.UtcNow;
        var employee = new Employee(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Porteiro Teste",
            "porteiro",
            "hash-valido-apenas-para-teste");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            employee.RegisterFailedLogin(now);
        }

        Assert.IsTrue(employee.IsLocked(now.AddMinutes(1)));
    }
}

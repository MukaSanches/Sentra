using Sentra.Domain.Access;

namespace Sentra.Domain.Tests.Access;

public sealed class AccessEntityTests
{
    [Fact]
    public void SystemRole_CannotBeRenamed()
    {
        var role = new Role(Guid.NewGuid(), "Proprietário", "samuel", isSystem: true);

        Assert.Throws<InvalidOperationException>(() =>
            role.Rename("Outro", "samuel", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Permission_RejectsUnknownCode()
    {
        Assert.Throws<ArgumentException>(() =>
            new Permission("unknown.permission", "Unknown"));
    }

    [Fact]
    public void Employee_RequiresIdentitySubject()
    {
        Assert.Throws<ArgumentException>(() =>
            new Employee(Guid.NewGuid(), " ", "Marcos", "samuel"));
    }
}

using Sentra.Domain.Properties;

namespace Sentra.Domain.Tests.Properties;

public sealed class PropertyEntityTests
{
    [Fact]
    public void Condominium_TrimsNameAndStartsActive()
    {
        var condominium = new Condominium("  Residencial Sentra  ", "samuel");

        Assert.Equal("Residencial Sentra", condominium.Name);
        Assert.True(condominium.IsActive);
        Assert.Equal("samuel", condominium.CreatedBy);
    }

    [Fact]
    public void Unit_RejectsEmptyBlockId()
    {
        Assert.Throws<ArgumentException>(() =>
            new Unit(Guid.NewGuid(), Guid.Empty, "72", "samuel"));
    }
}

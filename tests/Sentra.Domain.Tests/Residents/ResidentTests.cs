using Sentra.Domain.Residents;

namespace Sentra.Domain.Tests.Residents;

public sealed class ResidentTests
{
    [Theory]
    [InlineData("+5511999999999")]
    [InlineData("+442071838750")]
    public void ResidentPhone_AcceptsE164(string value)
    {
        var phone = new ResidentPhone(Guid.NewGuid(), value, true, true, "samuel");

        Assert.Equal(value, phone.E164Number);
    }

    [Theory]
    [InlineData("11999999999")]
    [InlineData("+55 11 99999-9999")]
    [InlineData("+abc")]
    public void ResidentPhone_RejectsNonE164(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            new ResidentPhone(Guid.NewGuid(), value, true, true, "samuel"));
    }

    [Fact]
    public void ResidentUnit_EndRequiresDateAfterStart()
    {
        var start = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
        var link = new ResidentUnit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ResidentUnitRole.Owner,
            true,
            "samuel",
            start);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            link.End(start, "samuel"));
    }
}

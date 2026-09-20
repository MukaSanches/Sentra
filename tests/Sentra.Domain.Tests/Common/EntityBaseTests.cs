using Sentra.Domain.Common;

namespace Sentra.Domain.Tests.Common;

public sealed class EntityBaseTests
{
    [Fact]
    public void MarkUpdated_AdvancesUpdatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.Zero);
        var entity = new TestEntity(createdAt);
        var updatedAt = createdAt.AddMinutes(5);

        entity.MarkUpdated(updatedAt);

        Assert.Equal(updatedAt, entity.UpdatedAt);
    }

    [Fact]
    public void MarkUpdated_RejectsTimestampEarlierThanCreation()
    {
        var createdAt = new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.Zero);
        var entity = new TestEntity(createdAt);

        Assert.Throws<ArgumentOutOfRangeException>(() => entity.MarkUpdated(createdAt.AddSeconds(-1)));
    }

    private sealed class TestEntity(DateTimeOffset createdAt) : EntityBase(createdAt)
    {
    }
}

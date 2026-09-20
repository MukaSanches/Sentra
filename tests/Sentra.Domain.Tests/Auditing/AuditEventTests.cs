using Sentra.Domain.Auditing;

namespace Sentra.Domain.Tests.Auditing;

public sealed class AuditEventTests
{
    [Fact]
    public void Constructor_NormalizesOptionalValues()
    {
        var timestamp = new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.Zero);
        var auditEvent = new AuditEvent(
            "  authorization.created ",
            " VisitorAuthorization ",
            " 123 ",
            " success ",
            timestamp,
            " porteiro-01 ",
            " correlation-01 ");

        Assert.Equal("authorization.created", auditEvent.Action);
        Assert.Equal("VisitorAuthorization", auditEvent.EntityType);
        Assert.Equal("123", auditEvent.EntityId);
        Assert.Equal("success", auditEvent.Outcome);
        Assert.Equal("porteiro-01", auditEvent.ActorId);
        Assert.Equal("correlation-01", auditEvent.CorrelationId);
    }

    [Fact]
    public void Constructor_RejectsMissingRequiredAction()
    {
        var timestamp = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() =>
            new AuditEvent(" ", "VisitorAuthorization", "123", "success", timestamp));
    }
}

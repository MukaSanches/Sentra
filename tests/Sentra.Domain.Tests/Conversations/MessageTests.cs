using Sentra.Domain.Conversations;

namespace Sentra.Domain.Tests.Conversations;

public sealed class MessageTests
{
    [Fact]
    public void DeliveryStatus_IgnoresOlderWebhookStatus()
    {
        var occurredAt = new DateTimeOffset(
            2026, 9, 20, 18, 0, 0, TimeSpan.Zero);
        var message = Message.CreateOutboundPending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Teste",
            occurredAt);

        message.MarkAccepted("wamid.1", occurredAt.AddSeconds(1));
        message.ApplyDeliveryStatus(
            MessageDeliveryStatus.Delivered,
            occurredAt.AddSeconds(10));

        var applied = message.ApplyDeliveryStatus(
            MessageDeliveryStatus.Sent,
            occurredAt.AddSeconds(5));

        Assert.False(applied);
        Assert.Equal(
            MessageDeliveryStatus.Delivered,
            message.DeliveryStatus);
    }

    [Fact]
    public void DeliveryStatus_DoesNotRegressEvenWithLaterTimestamp()
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var message = Message.CreateOutboundPending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Teste",
            occurredAt);

        message.MarkAccepted("wamid.1", occurredAt.AddSeconds(1));
        message.ApplyDeliveryStatus(
            MessageDeliveryStatus.Delivered,
            occurredAt.AddSeconds(2));

        var applied = message.ApplyDeliveryStatus(
            MessageDeliveryStatus.Sent,
            occurredAt.AddSeconds(3));

        Assert.False(applied);
        Assert.Equal(
            MessageDeliveryStatus.Delivered,
            message.DeliveryStatus);
    }

    [Fact]
    public void WebhookEvent_BecomesDeadLetterAtMaxAttempts()
    {
        var now = DateTimeOffset.UtcNow;
        var webhook = new Sentra.Domain.Integrations.WebhookEvent(
            "Meta",
            new string('A', 64),
            "{}",
            now);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            webhook.MarkProcessing(now.AddSeconds(attempt * 2 + 1));
            webhook.MarkFailed(
                "processing_failed",
                now.AddSeconds(attempt * 2 + 2),
                TimeSpan.FromSeconds(1),
                5);
        }

        Assert.Equal(
            Sentra.Domain.Integrations.WebhookEventStatus.DeadLetter,
            webhook.Status);
    }
}

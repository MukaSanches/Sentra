using Sentra.Domain.WhatsApp;

namespace Sentra.Domain.Tests.WhatsApp;

public sealed class WhatsAppConversationTests
{
    [Fact]
    public void CustomerServiceWindow_IsOpenForExactlyTwentyFourHours()
    {
        var inbound = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);
        var conversation = new WhatsAppConversation(
            Guid.NewGuid(),
            "5511999999999",
            "system",
            inbound);
        conversation.RecordInbound(inbound, null, "Morador");

        Assert.True(conversation.IsInsideCustomerServiceWindow(inbound.AddHours(24)));
        Assert.False(conversation.IsInsideCustomerServiceWindow(inbound.AddHours(24).AddTicks(1)));
    }

    [Fact]
    public void DeliveryStatus_IgnoresOlderWebhookTimestamp()
    {
        var time = DateTimeOffset.UtcNow;
        var message = new WhatsAppMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            WhatsAppMessageDirection.Outbound,
            WhatsAppMessageType.Text,
            time,
            "wamid.test");

        Assert.True(message.ApplyDeliveryStatus(WhatsAppDeliveryStatus.Delivered, time.AddSeconds(5)));
        Assert.False(message.ApplyDeliveryStatus(WhatsAppDeliveryStatus.Sent, time.AddSeconds(2)));
        Assert.Equal(WhatsAppDeliveryStatus.Delivered, message.DeliveryStatus);
    }
}

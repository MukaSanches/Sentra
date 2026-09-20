using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sentra.Domain.Integrations;

namespace Sentra.Domain.Tests;

[TestClass]
public sealed class WebhookEventTests
{
    [TestMethod]
    public void Constructor_RejectsMissingExternalEventId()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new WebhookEvent("Meta", " ", "abc123"));
    }

    [TestMethod]
    public void MarkProcessed_IsIdempotent()
    {
        var webhook = new WebhookEvent("Meta", "wamid.1", "abc123");

        webhook.MarkProcessed();
        var processedAt = webhook.ProcessedAt;
        webhook.MarkProcessed();

        Assert.AreEqual(WebhookProcessingStatus.Processed, webhook.Status);
        Assert.IsNotNull(processedAt);
        Assert.AreEqual(processedAt, webhook.ProcessedAt);
    }
}

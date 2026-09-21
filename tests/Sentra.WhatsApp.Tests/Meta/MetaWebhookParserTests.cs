using Sentra.Application.Integrations.WhatsApp;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp.Tests.Meta;

public sealed class MetaWebhookParserTests
{
    [Fact]
    public void Parse_OfficialTextShape_ReturnsMessage()
    {
        const string json = """
        {
          "object": "whatsapp_business_account",
          "entry": [{
            "id": "8856996819413533",
            "changes": [{
              "value": {
                "messaging_product": "whatsapp",
                "metadata": {
                  "display_phone_number": "16505553333",
                  "phone_number_id": "27681414235104944"
                },
                "contacts": [{
                  "profile": { "name": "Kerry Fisher" },
                  "wa_id": "16315551234"
                }],
                "messages": [{
                  "from": "16315551234",
                  "id": "wamid.ABGGFlCGg0cvAgo-sJQh43L5Pe4W",
                  "timestamp": "1603059201",
                  "text": { "body": "Hello this is an answer" },
                  "type": "text"
                }]
              },
              "field": "messages"
            }]
          }]
        }
        """;

        var parser = new MetaWebhookParser();

        var envelope = parser.Parse(json);

        var change = Assert.Single(envelope.Changes);
        Assert.Equal("27681414235104944", change.PhoneNumberId);
        Assert.Equal("Kerry Fisher", change.ContactName);
        var message = Assert.Single(change.Messages);
        Assert.Equal("16315551234", message.FromWaId);
        Assert.Equal(WhatsAppIncomingKind.Text, message.Kind);
        Assert.Equal("Hello this is an answer", message.Text);
    }

    [Fact]
    public void Parse_Status_PreservesTimestampAndError()
    {
        const string json = """
        {
          "object": "whatsapp_business_account",
          "entry": [{
            "changes": [{
              "value": {
                "metadata": { "phone_number_id": "27681414235104944" },
                "statuses": [{
                  "id": "wamid.1",
                  "status": "failed",
                  "timestamp": "1603059201",
                  "recipient_id": "16315551234",
                  "errors": [{ "code": 131000, "title": "Failure" }]
                }]
              }
            }]
          }]
        }
        """;

        var parser = new MetaWebhookParser();

        var status = Assert.Single(
            Assert.Single(parser.Parse(json).Changes).Statuses);

        Assert.Equal("failed", status.Status);
        Assert.Equal("131000", status.ErrorCode);
        Assert.Equal("Failure", status.ErrorTitle);
    }
}

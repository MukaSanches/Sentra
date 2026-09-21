using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Sentra.WhatsApp.Meta;

namespace Sentra.WhatsApp.Tests.Meta;

public sealed class MetaWebhookVerifierTests
{
    [Fact]
    public void VerifySignature_ValidHmac_ReturnsTrue()
    {
        var configuration = CreateConfiguration();
        var verifier = new MetaWebhookVerifier(configuration);
        var payload = Encoding.UTF8.GetBytes("{\"object\":\"whatsapp_business_account\"}");
        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"));
        var signature = "sha256=" +
            Convert.ToHexString(hmac.ComputeHash(payload)).ToLowerInvariant();

        Assert.True(verifier.VerifySignature(payload, signature));
        Assert.False(verifier.VerifySignature(payload, "sha256=00"));
    }

    [Fact]
    public void VerifyChallenge_RequiresSubscribeAndExactToken()
    {
        var verifier = new MetaWebhookVerifier(CreateConfiguration());

        Assert.True(verifier.VerifyChallenge("subscribe", "verify-token-123456"));
        Assert.False(verifier.VerifyChallenge("subscribe", "wrong"));
        Assert.False(verifier.VerifyChallenge("other", "verify-token-123456"));
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["META_GRAPH_VERSION"] = "v99.0",
                ["META_PHONE_NUMBER_ID"] = "123456",
                ["META_WABA_ID"] = "789012",
                ["META_ACCESS_TOKEN"] = "test-token",
                ["META_VERIFY_TOKEN"] = "verify-token-123456",
                ["META_APP_SECRET"] = "0123456789abcdef0123456789abcdef"
            })
            .Build();
}

using System.Security.Cryptography;
using System.Text;
using Sentra.WhatsApp.Security;

namespace Sentra.WhatsApp.Tests.Security;

public sealed class MetaWebhookSignatureValidatorTests
{
    [Fact]
    public void IsValid_AcceptsCorrectHmacSha256()
    {
        var body = Encoding.UTF8.GetBytes("""{"object":"whatsapp_business_account"}""");
        const string secret = "app-secret-test";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = "sha256=" + Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();

        Assert.True(MetaWebhookSignatureValidator.IsValid(body, signature, secret));
    }

    [Fact]
    public void IsValid_RejectsModifiedPayload()
    {
        var body = Encoding.UTF8.GetBytes("""{"object":"whatsapp_business_account"}""");
        const string secret = "app-secret-test";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = "sha256=" + Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();
        var modified = Encoding.UTF8.GetBytes("""{"object":"modified"}""");

        Assert.False(MetaWebhookSignatureValidator.IsValid(modified, signature, secret));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sha1=1234")]
    [InlineData("sha256=xyz")]
    public void IsValid_RejectsMalformedSignature(string? signature)
    {
        Assert.False(MetaWebhookSignatureValidator.IsValid(
            Encoding.UTF8.GetBytes("{}"),
            signature,
            "secret"));
    }
}

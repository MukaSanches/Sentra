using Microsoft.Extensions.Configuration;
using Sentra.WhatsApp;

namespace Sentra.WhatsApp.Tests;

public sealed class WhatsAppConfigurationDiagnosticsTests
{
    [Fact]
    public void Diagnostics_ListMissingKeysWithoutExposingSecretValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["META_GRAPH_VERSION"] = "v23.0",
                ["META_APP_ID"] = "123",
                ["META_WABA_ID"] = "456",
                ["META_PHONE_NUMBER_ID"] = "789",
                ["META_ACCESS_TOKEN"] = "super-sensitive-token"
            })
            .Build();

        var issues = configuration.GetSentraWhatsAppConfigurationIssues();

        Assert.Contains("META_VERIFY_TOKEN: ausente", issues);
        Assert.Contains("META_APP_SECRET: ausente", issues);
        Assert.DoesNotContain(
            issues,
            issue => issue.Contains("super-sensitive-token", StringComparison.Ordinal));
    }

    [Fact]
    public void Diagnostics_ReturnsNoIssuesForStructurallyValidConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["META_GRAPH_VERSION"] = "v23.0",
                ["META_APP_ID"] = "123",
                ["META_WABA_ID"] = "456",
                ["META_PHONE_NUMBER_ID"] = "789",
                ["META_ACCESS_TOKEN"] = "token-value",
                ["META_VERIFY_TOKEN"] = "verify-token-123456",
                ["META_APP_SECRET"] = "app-secret-123456789"
            })
            .Build();

        Assert.Empty(configuration.GetSentraWhatsAppConfigurationIssues());
    }

    [Fact]
    public void Diagnostics_DoesNotRequireMetaAppId()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["META_GRAPH_VERSION"] = "v23.0",
                ["META_WABA_ID"] = "456",
                ["META_PHONE_NUMBER_ID"] = "789",
                ["META_ACCESS_TOKEN"] = "token-value",
                ["META_VERIFY_TOKEN"] = "verify-token-123456",
                ["META_APP_SECRET"] = "app-secret-123456789"
            })
            .Build();

        Assert.Empty(configuration.GetSentraWhatsAppConfigurationIssues());
    }

    [Fact]
    public void Diagnostics_ReturnsStructuralValidationErrorWithoutSecretValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["META_GRAPH_VERSION"] = "latest",
                ["META_APP_ID"] = "123",
                ["META_WABA_ID"] = "456",
                ["META_PHONE_NUMBER_ID"] = "789",
                ["META_ACCESS_TOKEN"] = "token-value",
                ["META_VERIFY_TOKEN"] = "verify-token-123456",
                ["META_APP_SECRET"] = "app-secret-123456789"
            })
            .Build();

        var issue = Assert.Single(
            configuration.GetSentraWhatsAppConfigurationIssues());

        Assert.Contains("META_GRAPH_VERSION", issue, StringComparison.Ordinal);
        Assert.DoesNotContain("token-value", issue, StringComparison.Ordinal);
        Assert.DoesNotContain("app-secret-123456789", issue, StringComparison.Ordinal);
    }
}

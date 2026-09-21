using System.Text;
using Sentra.Infrastructure.Backup;

namespace Sentra.Infrastructure.Tests.Backup;

public sealed class TenantBackupCryptoTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrip_PreservesPayload()
    {
        var plaintext = Encoding.UTF8.GetBytes(
            "{\"format\":\"SENTRA-BACKUP-1\",\"resident\":\"Maria\"}");
        const string passphrase = "SenhaBackupMuitoForte2026!";

        var encrypted = TenantBackupService.Encrypt(plaintext, passphrase);
        var decrypted = TenantBackupService.Decrypt(encrypted, passphrase);

        Assert.NotEqual(plaintext, encrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WithWrongPassphrase_IsRejected()
    {
        var encrypted = TenantBackupService.Encrypt(
            Encoding.UTF8.GetBytes("conteudo sensivel"),
            "SenhaCorretaMuitoForte2026!");

        var exception = Assert.Throws<InvalidDataException>(() =>
            TenantBackupService.Decrypt(
                encrypted,
                "SenhaErradaMuitoForte2026!"));

        Assert.Contains("Senha incorreta", exception.Message);
    }

    [Fact]
    public void Decrypt_TamperedEnvelope_IsRejected()
    {
        var encrypted = TenantBackupService.Encrypt(
            Encoding.UTF8.GetBytes("conteudo sensivel"),
            "SenhaBackupMuitoForte2026!");

        var text = Encoding.UTF8.GetString(encrypted);
        var marker = "\"Ciphertext\":\"";
        var start = text.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        start += marker.Length;

        var replacement = text[start] == 'A' ? 'B' : 'A';
        var tampered = text[..start] + replacement + text[(start + 1)..];

        Assert.Throws<InvalidDataException>(() =>
            TenantBackupService.Decrypt(
                Encoding.UTF8.GetBytes(tampered),
                "SenhaBackupMuitoForte2026!"));
    }
}

namespace Sentra.Application.Backup;

public interface ITenantBackupService
{
    Task<byte[]> ExportAsync(
        Guid condominiumId,
        string passphrase,
        CancellationToken cancellationToken);

    Task RestoreAsync(
        Guid condominiumId,
        ReadOnlyMemory<byte> encryptedBackup,
        string passphrase,
        CancellationToken cancellationToken);
}

using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sentra.Application.Backup;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Infrastructure.Backup;

public sealed class TenantBackupService(SentraDbContext db) : ITenantBackupService
{
    private const int Pbkdf2Iterations = 210_000;
    private const int MaxPlaintextBytes = 100 * 1024 * 1024;

    private static readonly IReadOnlyList<TableDefinition> Tables =
    [
        new("blocks", "condominium_id = @condominium_id"),
        new("units", "condominium_id = @condominium_id"),
        new("residents", "id IN (SELECT ru.resident_id FROM resident_units ru JOIN units u ON u.id=ru.unit_id WHERE u.condominium_id=@condominium_id)"),
        new("resident_phones", "resident_id IN (SELECT ru.resident_id FROM resident_units ru JOIN units u ON u.id=ru.unit_id WHERE u.condominium_id=@condominium_id)"),
        new("resident_units", "unit_id IN (SELECT id FROM units WHERE condominium_id=@condominium_id)"),
        new("service_providers", "condominium_id = @condominium_id"),
        new("visitor_authorizations", "condominium_id = @condominium_id"),
        new("visits", "condominium_id = @condominium_id"),
        new("qr_credentials", "condominium_id = @condominium_id"),
        new("provider_authorizations", "condominium_id = @condominium_id"),
        new("packages", "condominium_id = @condominium_id"),
        new("occurrences", "condominium_id = @condominium_id"),
        new("shifts", "condominium_id = @condominium_id"),
        new("announcements", "condominium_id = @condominium_id"),
        new("conversations", "condominium_id = @condominium_id"),
        new("conversation_participants", "conversation_id IN (SELECT id FROM conversations WHERE condominium_id=@condominium_id)"),
        new("messages", "conversation_id IN (SELECT id FROM conversations WHERE condominium_id=@condominium_id)"),
        new("attachments", "message_id IN (SELECT m.id FROM messages m JOIN conversations c ON c.id=m.conversation_id WHERE c.condominium_id=@condominium_id)"),
        new("message_status_events", "external_message_id IN (SELECT m.external_message_id FROM messages m JOIN conversations c ON c.id=m.conversation_id WHERE c.condominium_id=@condominium_id AND m.external_message_id IS NOT NULL)"),
        new("ai_interpretations", "condominium_id = @condominium_id"),
        new("pending_actions", "condominium_id = @condominium_id")
    ];

    public async Task<byte[]> ExportAsync(
        Guid condominiumId,
        string passphrase,
        CancellationToken cancellationToken)
    {
        ValidatePassphrase(passphrase);

        var exists = await db.Condominiums.AsNoTracking()
            .AnyAsync(x => x.Id == condominiumId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Condomínio não encontrado.");

        var data = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (var table in Tables)
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText =
                    $"SELECT COALESCE(jsonb_agg(to_jsonb(t)), '[]'::jsonb)::text FROM {table.Name} t WHERE {table.Filter};";
                Add(command, "@condominium_id", condominiumId);

                var json = (string?)await command.ExecuteScalarAsync(cancellationToken) ?? "[]";
                using var document = JsonDocument.Parse(json);
                data[table.Name] = document.RootElement.Clone();
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(new TenantBackupPayload(
            "SENTRA-BACKUP-1",
            condominiumId,
            DateTimeOffset.UtcNow,
            data));

        if (payload.Length > MaxPlaintextBytes)
            throw new InvalidOperationException("Backup excede o limite operacional de 100 MB.");

        return Encrypt(payload, passphrase);
    }

    public async Task RestoreAsync(
        Guid condominiumId,
        ReadOnlyMemory<byte> encryptedBackup,
        string passphrase,
        CancellationToken cancellationToken)
    {
        ValidatePassphrase(passphrase);

        var plaintext = Decrypt(encryptedBackup.Span, passphrase);
        if (plaintext.Length > MaxPlaintextBytes)
            throw new InvalidDataException("Backup excede o limite permitido.");

        var payload = JsonSerializer.Deserialize<TenantBackupPayload>(plaintext)
            ?? throw new InvalidDataException("Backup inválido.");

        if (!string.Equals(payload.Format, "SENTRA-BACKUP-1", StringComparison.Ordinal))
            throw new InvalidDataException("Formato de backup não suportado.");

        if (payload.CondominiumId != condominiumId)
            throw new InvalidOperationException(
                "Este backup pertence a outro condomínio. A restauração cruzada é bloqueada.");

        var known = Tables.ToDictionary(x => x.Name, StringComparer.Ordinal);
        if (payload.Tables.Keys.Any(key => !known.ContainsKey(key)))
            throw new InvalidDataException("Backup contém tabela não reconhecida.");

        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var table in Tables)
            {
                if (!payload.Tables.TryGetValue(table.Name, out var rows) ||
                    rows.ValueKind != JsonValueKind.Array ||
                    rows.GetArrayLength() == 0)
                {
                    continue;
                }

                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.Transaction = transaction.GetDbTransaction();
                command.CommandText =
                    $"INSERT INTO {table.Name} SELECT * FROM jsonb_populate_recordset(NULL::{table.Name}, CAST(@json AS jsonb)) ON CONFLICT DO NOTHING;";
                Add(command, "@json", rows.GetRawText());
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    internal static byte[] Encrypt(ReadOnlySpan<byte> plaintext, string passphrase)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            passphrase,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            32);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(key, 16))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        CryptographicOperations.ZeroMemory(key);

        return JsonSerializer.SerializeToUtf8Bytes(new EncryptedBackupEnvelope(
            "SENTRA-ENC-1",
            Pbkdf2Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(ciphertext)));
    }

    internal static byte[] Decrypt(ReadOnlySpan<byte> encryptedBackup, string passphrase)
    {
        EncryptedBackupEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EncryptedBackupEnvelope>(encryptedBackup)
                ?? throw new InvalidDataException("Envelope de backup vazio.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Arquivo de backup inválido.", exception);
        }

        if (!string.Equals(envelope.Format, "SENTRA-ENC-1", StringComparison.Ordinal) ||
            envelope.Iterations < 100_000 ||
            envelope.Iterations > 1_000_000)
            throw new InvalidDataException("Envelope de backup não suportado.");

        try
        {
            var salt = Convert.FromBase64String(envelope.Salt);
            var nonce = Convert.FromBase64String(envelope.Nonce);
            var tag = Convert.FromBase64String(envelope.Tag);
            var ciphertext = Convert.FromBase64String(envelope.Ciphertext);

            if (ciphertext.Length > MaxPlaintextBytes)
                throw new InvalidDataException("Backup excede o limite permitido.");

            var key = Rfc2898DeriveBytes.Pbkdf2(
                passphrase,
                salt,
                envelope.Iterations,
                HashAlgorithmName.SHA256,
                32);

            var plaintext = new byte[ciphertext.Length];
            try
            {
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                return plaintext;
            }
            catch (CryptographicException exception)
            {
                throw new InvalidDataException(
                    "Senha incorreta ou arquivo de backup adulterado.",
                    exception);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("Envelope de backup corrompido.", exception);
        }
    }

    private static void ValidatePassphrase(string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase) || passphrase.Length < 12)
            throw new ArgumentException(
                "A senha do backup deve possuir pelo menos 12 caracteres.",
                nameof(passphrase));
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record TableDefinition(string Name, string Filter);

    private sealed record TenantBackupPayload(
        string Format,
        Guid CondominiumId,
        DateTimeOffset ExportedAt,
        Dictionary<string, JsonElement> Tables);

    private sealed record EncryptedBackupEnvelope(
        string Format,
        int Iterations,
        string Salt,
        string Nonce,
        string Tag,
        string Ciphertext);
}

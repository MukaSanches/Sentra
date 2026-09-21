using System.Security.Claims;
using Sentra.Application.Backup;
using Sentra.Application.Security;

namespace Sentra.Api.Endpoints;

public static class BackupEndpoints
{
    private const long MaxBackupBytes = 130 * 1024 * 1024;

    public static IEndpointRouteBuilder MapSentraBackupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/backup", ExportAsync)
            .RequireAuthorization(PermissionCatalog.BackupManage);

        endpoints.MapPost("/api/v1/admin/backup/restore", RestoreAsync)
            .DisableAntiforgery()
            .RequireAuthorization(PermissionCatalog.BackupManage);

        return endpoints;
    }

    private static async Task<IResult> ExportAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        ITenantBackupService backup,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var condominiumId))
            return Results.Unauthorized();

        var passphrase = request.Headers["X-Sentra-Backup-Passphrase"].ToString();
        try
        {
            var bytes = await backup.ExportAsync(condominiumId, passphrase, ct);
            var name = $"SENTRA-backup-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.sentra";
            return Results.File(
                bytes,
                "application/vnd.sentra.backup",
                name,
                enableRangeProcessing: false);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    private static async Task<IResult> RestoreAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        ITenantBackupService backup,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var condominiumId))
            return Results.Unauthorized();

        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Envie multipart/form-data." });

        var form = await request.ReadFormAsync(ct);
        if (form.Files.Count != 1)
            return Results.BadRequest(new { error = "Envie exatamente um arquivo .sentra." });

        var file = form.Files[0];
        if (file.Length <= 0 || file.Length > MaxBackupBytes)
            return Results.BadRequest(new { error = "Backup vazio ou maior que 130 MB." });

        var passphrase = request.Headers["X-Sentra-Backup-Passphrase"].ToString();
        try
        {
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
            await stream.CopyToAsync(memory, ct);

            await backup.RestoreAsync(
                condominiumId,
                memory.ToArray(),
                passphrase,
                ct);

            return Results.Ok(new
            {
                restored = true,
                mode = "merge",
                message = "Restauração concluída sem exclusão de registros existentes."
            });
        }
        catch (Exception e) when (e is ArgumentException or InvalidDataException or InvalidOperationException)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    private static bool TryCondominium(ClaimsPrincipal user, out Guid condominiumId)
        => Guid.TryParse(user.FindFirst("condominium_id")?.Value, out condominiumId);
}

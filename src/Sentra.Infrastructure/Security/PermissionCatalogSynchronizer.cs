using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentra.Application.Security;
using Sentra.Domain.Security;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Infrastructure.Security;

internal sealed class PermissionCatalogSynchronizer(
    IServiceScopeFactory scopeFactory,
    ILogger<PermissionCatalogSynchronizer> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SentraDbContext>();

            var existing = await db.Permissions
                .ToListAsync(cancellationToken);

            var byCode = existing.ToDictionary(
                permission => permission.Code,
                StringComparer.Ordinal);

            foreach (var definition in PermissionCatalog.All)
            {
                if (!byCode.ContainsKey(definition.Code))
                {
                    var permission = new Permission(
                        definition.Code,
                        definition.Description);
                    db.Permissions.Add(permission);
                    existing.Add(permission);
                    byCode[definition.Code] = permission;
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            var administratorRoles = await db.Roles
                .Where(role => role.Name == "Administrador")
                .ToListAsync(cancellationToken);

            if (administratorRoles.Count == 0)
            {
                return;
            }

            var adminIds = administratorRoles
                .Select(role => role.Id)
                .ToArray();

            var existingLinks = await db.RolePermissions
                .Where(link => adminIds.Contains(link.RoleId))
                .ToListAsync(cancellationToken);

            var links = existingLinks
                .Select(link => (link.RoleId, link.PermissionId))
                .ToHashSet();

            foreach (var role in administratorRoles)
            {
                foreach (var permission in existing)
                {
                    if (links.Add((role.Id, permission.Id)))
                    {
                        db.RolePermissions.Add(
                            new RolePermission(role.Id, permission.Id));
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Não foi possível sincronizar o catálogo de permissões. Verifique se as migrations foram aplicadas.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}

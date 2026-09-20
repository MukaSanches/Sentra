using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Security;
using Sentra.Domain.Condominiums;
using Sentra.Domain.Security;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public sealed record BootstrapRequest(
    string CondominiumName,
    string AdministratorName,
    string Username,
    string Password);

public static class SetupEndpoints
{
    public static IEndpointRouteBuilder MapSentraSetupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/setup/status", async (SentraDbContext db, CancellationToken cancellationToken) =>
        {
            var bootstrapped = await db.Employees.AnyAsync(cancellationToken);
            return Results.Ok(new { bootstrapped });
        }).AllowAnonymous();

        endpoints.MapPost("/api/v1/setup/bootstrap", BootstrapAsync).AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> BootstrapAsync(
        BootstrapRequest request,
        HttpRequest httpRequest,
        IConfiguration configuration,
        SentraDbContext db,
        IPasswordHashService passwordHashService,
        CancellationToken cancellationToken)
    {
        var configuredToken = configuration["SENTRA_BOOTSTRAP_TOKEN"];
        if (string.IsNullOrWhiteSpace(configuredToken) || configuredToken.Length < 32)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Bootstrap não configurado",
                detail: "Configure SENTRA_BOOTSTRAP_TOKEN com pelo menos 32 caracteres no servidor.");
        }

        var suppliedToken = httpRequest.Headers["X-Sentra-Bootstrap-Token"].ToString();
        if (!SecureEquals(configuredToken, suppliedToken))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
        {
            return Results.BadRequest(new { error = "A senha deve possuir pelo menos 12 caracteres." });
        }

        if (await db.Condominiums.AnyAsync(cancellationToken) || await db.Employees.AnyAsync(cancellationToken))
        {
            return Results.Conflict(new { error = "O bootstrap inicial já foi executado." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var condominium = new Condominium(request.CondominiumName);
        var adminRole = new Role(condominium.Id, "Administrador", "Perfil inicial com permissões administrativas.");

        var permissions = PermissionCatalog.All
            .Select(definition => new Permission(definition.Code, definition.Description))
            .ToArray();

        var passwordHash = passwordHashService.Hash(request.Password);
        var administrator = new Employee(
            condominium.Id,
            adminRole.Id,
            request.AdministratorName,
            request.Username,
            passwordHash);

        db.Condominiums.Add(condominium);
        db.Roles.Add(adminRole);
        db.Permissions.AddRange(permissions);
        db.RolePermissions.AddRange(permissions.Select(permission => new RolePermission(adminRole.Id, permission.Id)));
        db.Employees.Add(administrator);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Created(
            "/api/v1/setup/status",
            new
            {
                condominiumId = condominium.Id,
                administratorId = administrator.Id,
                message = "Configuração inicial concluída."
            });
    }

    private static bool SecureEquals(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);

        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}

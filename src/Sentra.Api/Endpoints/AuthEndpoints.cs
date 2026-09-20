using Microsoft.EntityFrameworkCore;
using Sentra.Api.Security;
using Sentra.Application.Security;
using Sentra.Domain.Security;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public sealed record LoginRequest(Guid CondominiumId, string Username, string Password);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapSentraAuthEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        endpoints.MapPost("/api/v1/auth/login", async (
            LoginRequest request,
            IConfiguration configuration,
            SentraDbContext db,
            IPasswordHashService passwordHashService,
            CancellationToken cancellationToken) =>
        {
            if (!authenticationConfigured)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Autenticação não configurada");
            }

            if (request.CondominiumId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrEmpty(request.Password))
            {
                return Results.Unauthorized();
            }

            var normalizedUsername = Employee.NormalizeUsername(request.Username);
            var now = DateTimeOffset.UtcNow;

            var employee = await db.Employees.SingleOrDefaultAsync(
                candidate =>
                    candidate.CondominiumId == request.CondominiumId &&
                    candidate.NormalizedUsername == normalizedUsername,
                cancellationToken);

            if (employee is null || !employee.IsActive || employee.IsLocked(now))
            {
                return Results.Unauthorized();
            }

            if (!passwordHashService.Verify(request.Password, employee.PasswordHash))
            {
                employee.RegisterFailedLogin(now);
                await db.SaveChangesAsync(cancellationToken);
                return Results.Unauthorized();
            }

            employee.RegisterSuccessfulLogin();

            var permissions = await (
                from rolePermission in db.RolePermissions
                join permission in db.Permissions on rolePermission.PermissionId equals permission.Id
                where rolePermission.RoleId == employee.RoleId
                select permission.Code)
                .ToListAsync(cancellationToken);

            await db.SaveChangesAsync(cancellationToken);

            var token = JwtTokenIssuer.Issue(configuration, employee, permissions, now);

            return Results.Ok(new
            {
                accessToken = token.AccessToken,
                expiresAt = token.ExpiresAt,
                employee = new
                {
                    employee.Id,
                    employee.FullName,
                    employee.Username,
                    employee.CondominiumId,
                    employee.RoleId
                },
                permissions
            });
        }).AllowAnonymous();

        return endpoints;
    }
}

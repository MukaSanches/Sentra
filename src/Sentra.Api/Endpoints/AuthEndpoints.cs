using Microsoft.EntityFrameworkCore;
using Sentra.Api.Security;
using Sentra.Application.Abstractions;
using Sentra.Application.Security;
using Sentra.Contracts.Auth;
using Sentra.Domain.Security;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapSentraAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/v1/auth/login",
            async (
                LoginRequest request,
                IConfiguration configuration,
                SentraDbContext db,
                IPasswordHashService passwordHashService,
                IClock clock,
                CancellationToken cancellationToken) =>
            {
                if (request.CondominiumId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(request.Username) ||
                    string.IsNullOrEmpty(request.Password))
                {
                    return Results.Unauthorized();
                }

                var normalizedUsername =
                    Employee.NormalizeUsername(request.Username);
                var now = clock.UtcNow;

                var employee = await db.Employees.SingleOrDefaultAsync(
                    candidate =>
                        candidate.CondominiumId == request.CondominiumId &&
                        candidate.NormalizedUsername == normalizedUsername,
                    cancellationToken);

                if (employee is null ||
                    !employee.IsActive ||
                    employee.IsLocked(now))
                {
                    return Results.Unauthorized();
                }

                if (!passwordHashService.Verify(
                        request.Password,
                        employee.PasswordHash))
                {
                    employee.RegisterFailedLogin(now);
                    await db.SaveChangesAsync(cancellationToken);
                    return Results.Unauthorized();
                }

                employee.RegisterSuccessfulLogin(now);

                var permissions = await (
                    from rolePermission in db.RolePermissions
                    join permission in db.Permissions
                        on rolePermission.PermissionId equals permission.Id
                    where rolePermission.RoleId == employee.RoleId
                    orderby permission.Code
                    select permission.Code)
                    .ToListAsync(cancellationToken);

                await db.SaveChangesAsync(cancellationToken);

                var response = JwtTokenIssuer.Issue(
                    configuration,
                    employee,
                    permissions,
                    now);

                return Results.Ok(response);
            })
            .AllowAnonymous();

        return endpoints;
    }
}

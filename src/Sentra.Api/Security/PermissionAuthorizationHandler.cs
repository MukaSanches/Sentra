using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Sentra.Application.Security;

namespace Sentra.Api.Security;

public sealed class PermissionAuthorizationHandler(
    IPermissionEvaluator permissionEvaluator,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var subject = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(subject))
        {
            return;
        }

        var rawCondominiumId = httpContext.Request.RouteValues["condominiumId"]?.ToString();
        if (!Guid.TryParse(rawCondominiumId, out var condominiumId))
        {
            return;
        }

        if (await permissionEvaluator.HasPermissionAsync(
            subject,
            condominiumId,
            requirement.PermissionCode,
            httpContext.RequestAborted))
        {
            context.Succeed(requirement);
        }
    }
}

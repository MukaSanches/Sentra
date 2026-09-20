using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Sentra.Api.Security;
using Sentra.Application.Security;

namespace Sentra.Api.Tests.Security;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_Succeeds_WhenDatabaseEvaluatorAllows()
    {
        var condominiumId = Guid.NewGuid();
        var httpContext = CreateHttpContext(condominiumId);
        var requirement = new PermissionRequirement("residents.read");
        var evaluator = new FakePermissionEvaluator(true);
        var handler = new PermissionAuthorizationHandler(
            evaluator,
            new HttpContextAccessor { HttpContext = httpContext });
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("sub", "employee-01") }, "test"));
        var authorizationContext = new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            resource: null);

        await handler.HandleAsync(authorizationContext);

        Assert.True(authorizationContext.HasSucceeded);
        Assert.Equal("employee-01", evaluator.Subject);
        Assert.Equal(condominiumId, evaluator.CondominiumId);
        Assert.Equal("residents.read", evaluator.PermissionCode);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSucceed_WhenEvaluatorDenies()
    {
        var requirement = new PermissionRequirement("residents.write");
        var handler = new PermissionAuthorizationHandler(
            new FakePermissionEvaluator(false),
            new HttpContextAccessor { HttpContext = CreateHttpContext(Guid.NewGuid()) });
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("sub", "employee-01") }, "test"));
        var authorizationContext = new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            resource: null);

        await handler.HandleAsync(authorizationContext);

        Assert.False(authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSucceed_WithoutCondominiumRoute()
    {
        var requirement = new PermissionRequirement("residents.read");
        var handler = new PermissionAuthorizationHandler(
            new FakePermissionEvaluator(true),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("sub", "employee-01") }, "test"));
        var authorizationContext = new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            resource: null);

        await handler.HandleAsync(authorizationContext);

        Assert.False(authorizationContext.HasSucceeded);
    }

    private static DefaultHttpContext CreateHttpContext(Guid condominiumId)
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["condominiumId"] = condominiumId.ToString();
        return context;
    }

    private sealed class FakePermissionEvaluator(bool allowed) : IPermissionEvaluator
    {
        public string? Subject { get; private set; }
        public Guid CondominiumId { get; private set; }
        public string? PermissionCode { get; private set; }

        public Task<bool> HasPermissionAsync(
            string identitySubject,
            Guid condominiumId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            Subject = identitySubject;
            CondominiumId = condominiumId;
            PermissionCode = permissionCode;
            return Task.FromResult(allowed);
        }
    }
}

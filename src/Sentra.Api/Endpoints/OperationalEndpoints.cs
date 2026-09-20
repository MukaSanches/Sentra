using System.Security.Claims;
using Sentra.Application.Operations;
using Sentra.Contracts.Operations;
using Sentra.Domain.Access;

namespace Sentra.Api.Endpoints;

public static class OperationalEndpoints
{
    public static IEndpointRouteBuilder MapOperationalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/setup/status", async (
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.GetSetupStatusAsync(cancellationToken)))
            .AllowAnonymous();

        endpoints.MapPost("/api/setup/bootstrap", Bootstrap)
            .RequireAuthorization();

        var condominium = endpoints.MapGroup("/api/condominiums/{condominiumId:guid}");

        condominium.MapGet("/", async (
            Guid condominiumId,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetCondominiumAsync(condominiumId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(PermissionCodes.CondominiumRead);

        condominium.MapPut("/", async (
            Guid condominiumId,
            UpdateCondominiumRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateCondominiumAsync(
                condominiumId,
                request,
                Actor(context),
                Correlation(context),
                cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(PermissionCodes.CondominiumWrite);

        condominium.MapGet("/blocks", async (
            Guid condominiumId,
            int page,
            int pageSize,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.ListBlocksAsync(condominiumId, page, pageSize, cancellationToken)))
            .RequireAuthorization(PermissionCodes.CondominiumRead);

        condominium.MapPost("/blocks", async (
            Guid condominiumId,
            CreateBlockRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.CreateBlockAsync(
                    condominiumId, request, Actor(context), Correlation(context), cancellationToken)))
            .RequireAuthorization(PermissionCodes.CondominiumWrite);

        condominium.MapPut("/blocks/{blockId:guid}", async (
            Guid condominiumId,
            Guid blockId,
            UpdateBlockRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateBlockAsync(
                condominiumId, blockId, request, Actor(context), Correlation(context), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(PermissionCodes.CondominiumWrite);

        condominium.MapGet("/units", async (
            Guid condominiumId,
            int page,
            int pageSize,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.ListUnitsAsync(condominiumId, page, pageSize, cancellationToken)))
            .RequireAuthorization(PermissionCodes.CondominiumRead);

        condominium.MapPost("/units", async (
            Guid condominiumId,
            CreateUnitRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.CreateUnitAsync(
                    condominiumId, request, Actor(context), Correlation(context), cancellationToken)))
            .RequireAuthorization(PermissionCodes.CondominiumWrite);

        condominium.MapPut("/units/{unitId:guid}", async (
            Guid condominiumId,
            Guid unitId,
            UpdateUnitRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateUnitAsync(
                condominiumId, unitId, request, Actor(context), Correlation(context), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(PermissionCodes.CondominiumWrite);

        condominium.MapGet("/residents", async (
            Guid condominiumId,
            string? search,
            int page,
            int pageSize,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.ListResidentsAsync(
                    condominiumId, search, page, pageSize, cancellationToken)))
            .RequireAuthorization(PermissionCodes.ResidentsRead);

        condominium.MapPost("/residents", async (
            Guid condominiumId,
            CreateResidentRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.CreateResidentAsync(
                    condominiumId, request, Actor(context), Correlation(context), cancellationToken)))
            .RequireAuthorization(PermissionCodes.ResidentsWrite);

        condominium.MapPut("/residents/{residentId:guid}", async (
            Guid condominiumId,
            Guid residentId,
            UpdateResidentRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateResidentAsync(
                condominiumId, residentId, request, Actor(context), Correlation(context), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(PermissionCodes.ResidentsWrite);

        condominium.MapPost("/residents/{residentId:guid}/phones", async (
            Guid condominiumId,
            Guid residentId,
            AddResidentPhoneRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AddResidentPhoneAsync(
                condominiumId, residentId, request, Actor(context), Correlation(context), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(PermissionCodes.ResidentsWrite);

        condominium.MapPost("/residents/{residentId:guid}/units", async (
            Guid condominiumId,
            Guid residentId,
            LinkResidentUnitRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.LinkResidentUnitAsync(
                condominiumId, residentId, request, Actor(context), Correlation(context), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(PermissionCodes.ResidentsWrite);

        condominium.MapGet("/employees", async (
            Guid condominiumId,
            int page,
            int pageSize,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.ListEmployeesAsync(condominiumId, page, pageSize, cancellationToken)))
            .RequireAuthorization(PermissionCodes.EmployeesRead);

        condominium.MapGet("/roles", async (
            Guid condominiumId,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.ListRolesAsync(condominiumId, cancellationToken)))
            .RequireAuthorization(PermissionCodes.EmployeesRead);

        condominium.MapPost("/roles", async (
            Guid condominiumId,
            CreateRoleRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.CreateRoleAsync(
                    condominiumId, request, Actor(context), Correlation(context), cancellationToken)))
            .RequireAuthorization(PermissionCodes.RolesManage);

        condominium.MapPut("/roles/{roleId:guid}/permissions", async (
            Guid condominiumId,
            Guid roleId,
            SetRolePermissionsRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var updated = await service.SetRolePermissionsAsync(
                condominiumId, roleId, request, Actor(context), Correlation(context), cancellationToken);
            return updated ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(PermissionCodes.RolesManage);

        condominium.MapPost("/employees/{employeeId:guid}/roles", async (
            Guid condominiumId,
            Guid employeeId,
            AssignEmployeeRoleRequest request,
            HttpContext context,
            IOperationalDataService service,
            CancellationToken cancellationToken) =>
        {
            var updated = await service.AssignEmployeeRoleAsync(
                condominiumId, employeeId, request, Actor(context), Correlation(context), cancellationToken);
            return updated ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(PermissionCodes.EmployeesManage);

        return endpoints;
    }

    private static async Task<IResult> Bootstrap(
        BootstrapRequest request,
        HttpContext context,
        IOperationalDataService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.BootstrapAsync(
                request,
                Actor(context),
                Correlation(context),
                cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }
    }

    private static string Actor(HttpContext context)
        => context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Usuário autenticado sem subject.");

    private static string? Correlation(HttpContext context)
        => context.Items.TryGetValue("CorrelationId", out var value) ? value?.ToString() : null;
}

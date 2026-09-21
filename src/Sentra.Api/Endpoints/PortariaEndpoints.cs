using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Sentra.Application.Operations;
using Sentra.Application.Security;
using Sentra.Contracts.Operations;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public static class PortariaEndpoints
{
    public static IEndpointRouteBuilder MapSentraPortariaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1");

        group.MapGet("/dashboard", DashboardAsync).RequireAuthorization(PermissionCatalog.DashboardRead);

        group.MapGet("/visitors/authorizations", ListVisitorsAsync).RequireAuthorization(PermissionCatalog.VisitorsRead);
        group.MapPost("/visitors/authorizations", CreateVisitorAsync).RequireAuthorization(PermissionCatalog.VisitorsManage);
        group.MapPost("/visitors/authorizations/{id:guid}/visit", VisitAsync).RequireAuthorization(PermissionCatalog.VisitorsManage);
        group.MapPost("/visitors/authorizations/{id:guid}/qr", CreateQrAsync).RequireAuthorization(PermissionCatalog.VisitorsManage);
        group.MapPost("/visitors/qr/validate", ValidateQrAsync).RequireAuthorization(PermissionCatalog.VisitorsRead);

        group.MapGet("/providers", ListProvidersAsync).RequireAuthorization(PermissionCatalog.ProvidersRead);
        group.MapPost("/providers", CreateProviderAsync).RequireAuthorization(PermissionCatalog.ProvidersManage);
        group.MapPost("/providers/authorizations", CreateProviderAuthorizationAsync).RequireAuthorization(PermissionCatalog.ProvidersManage);

        group.MapGet("/packages", ListPackagesAsync).RequireAuthorization(PermissionCatalog.PackagesRead);
        group.MapPost("/packages", CreatePackageAsync).RequireAuthorization(PermissionCatalog.PackagesManage);
        group.MapPost("/packages/{id:guid}/collect", CollectPackageAsync).RequireAuthorization(PermissionCatalog.PackagesManage);

        group.MapGet("/occurrences", ListOccurrencesAsync).RequireAuthorization(PermissionCatalog.OccurrencesRead);
        group.MapPost("/occurrences", CreateOccurrenceAsync).RequireAuthorization(PermissionCatalog.OccurrencesManage);
        group.MapPut("/occurrences/{id:guid}", UpdateOccurrenceAsync).RequireAuthorization(PermissionCatalog.OccurrencesManage);

        group.MapGet("/shifts", ListShiftsAsync).RequireAuthorization(PermissionCatalog.ShiftsRead);
        group.MapPost("/shifts/open", OpenShiftAsync).RequireAuthorization(PermissionCatalog.ShiftsManage);
        group.MapPost("/shifts/{id:guid}/close", CloseShiftAsync).RequireAuthorization(PermissionCatalog.ShiftsManage);
        group.MapPost("/shifts/{id:guid}/acknowledge", AcknowledgeShiftAsync).RequireAuthorization(PermissionCatalog.ShiftsManage);

        group.MapGet("/announcements", ListAnnouncementsAsync).RequireAuthorization(PermissionCatalog.AnnouncementsRead);
        group.MapPost("/announcements", CreateAnnouncementAsync).RequireAuthorization(PermissionCatalog.AnnouncementsManage);

        group.MapGet("/search", SearchAsync).RequireAuthorization(PermissionCatalog.DashboardRead);
        group.MapGet("/offline/snapshot", OfflineSnapshotAsync).RequireAuthorization(PermissionCatalog.DashboardRead);
        group.MapGet("/audit", AuditAsync).RequireAuthorization(PermissionCatalog.AuditRead);

        return endpoints;
    }

    private static async Task<IResult> DashboardAsync(ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryIds(user, out var condominiumId, out var employeeId)) return Results.Unauthorized();
        return Results.Ok(await store.GetDashboardAsync(condominiumId, employeeId, ct));
    }

    private static async Task<IResult> ListVisitorsAsync(bool? activeOnly, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        return Results.Ok(await store.ListVisitorAuthorizationsAsync(c, activeOnly ?? true, ct));
    }

    private static async Task<IResult> CreateVisitorAsync(CreateVisitorAuthorizationRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        try { return Results.Ok(await store.CreateVisitorAuthorizationAsync(c, request, ct)); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
    }

    private static async Task<IResult> VisitAsync(Guid id, RegisterVisitRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryIds(user, out var c, out var employee)) return Results.Unauthorized();
        try
        {
            var result = await store.RegisterVisitAsync(c, id, employee, request.Action, ct);
            return result is null ? Results.Conflict(new { error = "A autorização não está em estado válido para esta ação." }) : Results.Ok(result);
        }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
    }

    private static async Task<IResult> CreateQrAsync(Guid id, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        try
        {
            var auth = await store.GetVisitorAuthorizationAsync(c, id, ct);
            if (auth is null) return Results.NotFound();
            var expires = auth.EndsAt < DateTimeOffset.UtcNow.AddHours(12) ? auth.EndsAt : DateTimeOffset.UtcNow.AddHours(12);
            var qr = await store.CreateQrCredentialAsync(c, id, true, expires, ct);

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(qr.Payload, QRCodeGenerator.ECCLevel.Q);
            using var renderer = new SvgQRCode(data);
            var svg = renderer.GetGraphic(4);

            return Results.Ok(new QrCredentialResponse(qr.Id, id, qr.Payload, svg, qr.ExpiresAt, qr.OneTime));
        }
        catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
    }

    private static async Task<IResult> ValidateQrAsync(ValidateQrRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        var auth = await store.ValidateQrAsync(c, request.Payload, ct);
        return Results.Ok(new ValidateQrResponse(auth is not null, auth is null ? "NÃO LIBERAR" : "AUTORIZAÇÃO VÁLIDA", auth));
    }

    private static async Task<IResult> ListProvidersAsync(ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        return Results.Ok(await store.ListProvidersAsync(c, ct));
    }

    private static async Task<IResult> CreateProviderAsync(CreateServiceProviderRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        try { return Results.Ok(await store.CreateProviderAsync(c, request, ct)); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
    }

    private static async Task<IResult> CreateProviderAuthorizationAsync(CreateProviderAuthorizationRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        try { return Results.Ok(await store.CreateProviderAuthorizationAsync(c, request, ct)); }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException) { return Results.BadRequest(new { error = e.Message }); }
    }

    private static async Task<IResult> ListPackagesAsync(bool? pendingOnly, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        return Results.Ok(await store.ListPackagesAsync(c, pendingOnly ?? true, ct));
    }

    private static async Task<IResult> CreatePackageAsync(CreatePackageRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryIds(user, out var c, out var employee)) return Results.Unauthorized();
        try { return Results.Ok(await store.CreatePackageAsync(c, employee, request, ct)); }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException) { return Results.BadRequest(new { error = e.Message }); }
    }

    private static async Task<IResult> CollectPackageAsync(Guid id, CollectPackageRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryIds(user, out var c, out var employee)) return Results.Unauthorized();
        var result = await store.CollectPackageAsync(c, id, employee, request.PickupCode, ct);
        return result is null ? Results.Conflict(new { error = "Encomenda já retirada ou inexistente." }) : Results.Ok(result);
    }

    private static async Task<IResult> ListOccurrencesAsync(bool? openOnly, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        return Results.Ok(await store.ListOccurrencesAsync(c, openOnly ?? true, ct));
    }

    private static async Task<IResult> CreateOccurrenceAsync(CreateOccurrenceRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        try { return Results.Ok(await store.CreateOccurrenceAsync(c, request, ct)); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
    }

    private static async Task<IResult> UpdateOccurrenceAsync(Guid id, UpdateOccurrenceRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        var result = await store.UpdateOccurrenceAsync(c, id, request, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ListShiftsAsync(ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        return Results.Ok(await store.ListShiftsAsync(c, ct));
    }

    private static async Task<IResult> OpenShiftAsync(OpenShiftRequest _, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryIds(user, out var c, out var employee)) return Results.Unauthorized();
        return Results.Ok(await store.OpenShiftAsync(c, employee, ct));
    }

    private static async Task<IResult> CloseShiftAsync(Guid id, CloseShiftRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryIds(user, out var c, out var employee)) return Results.Unauthorized();
        var result = await store.CloseShiftAsync(c, id, employee, request.HandoffSummary, ct);
        return result is null ? Results.Conflict(new { error = "Turno não pode ser encerrado." }) : Results.Ok(result);
    }

    private static async Task<IResult> AcknowledgeShiftAsync(Guid id, AcknowledgeShiftRequest _, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryIds(user, out var c, out var employee)) return Results.Unauthorized();
        var result = await store.AcknowledgeShiftAsync(c, id, employee, ct);
        return result is null ? Results.Conflict(new { error = "Turno já assumido ou ainda não encerrado." }) : Results.Ok(result);
    }

    private static async Task<IResult> ListAnnouncementsAsync(ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        return Results.Ok(await store.ListAnnouncementsAsync(c, ct));
    }

    private static async Task<IResult> CreateAnnouncementAsync(CreateAnnouncementRequest request, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        try { return Results.Ok(await store.CreateAnnouncementAsync(c, request, ct)); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
    }

    private static async Task<IResult> SearchAsync(string q, ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(q)) return Results.Ok(Array.Empty<SearchResultResponse>());
        return Results.Ok(await store.SearchAsync(c, q, ct));
    }

    private static async Task<IResult> OfflineSnapshotAsync(ClaimsPrincipal user, IOperationalStore store, CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        return Results.Ok(await store.GetOfflineSnapshotAsync(c, ct));
    }

    private static async Task<IResult> AuditAsync(
        int? take,
        ClaimsPrincipal user,
        SentraDbContext db,
        CancellationToken ct)
    {
        if (!TryCondominium(user, out var c)) return Results.Unauthorized();
        var employeeIds = await db.Employees.AsNoTracking()
            .Where(x => x.CondominiumId == c)
            .Select(x => x.Id.ToString())
            .ToListAsync(ct);

        var limit = Math.Clamp(take ?? 200, 1, 1000);
        var rows = await db.AuditEvents.AsNoTracking()
            .Where(x => x.ActorId == null || employeeIds.Contains(x.ActorId))
            .OrderByDescending(x => x.OccurredAt)
            .Take(limit)
            .Select(x => new AuditRecordResponse(x.Id, x.Action, x.EntityType, x.EntityId, x.Outcome, x.OccurredAt, x.ActorId, x.CorrelationId))
            .ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static bool TryCondominium(ClaimsPrincipal user, out Guid condominiumId)
        => Guid.TryParse(user.FindFirst("condominium_id")?.Value, out condominiumId);

    private static bool TryIds(ClaimsPrincipal user, out Guid condominiumId, out Guid employeeId)
    {
        condominiumId = Guid.Empty;
        employeeId = Guid.Empty;
        var employee = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value;
        return TryCondominium(user, out condominiumId)
            && Guid.TryParse(employee, out employeeId);
    }
}

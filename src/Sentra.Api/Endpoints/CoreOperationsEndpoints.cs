using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Security;
using Sentra.Domain.Condominiums;
using Sentra.Domain.Residents;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public sealed record CreateBlockRequest(string Name);
public sealed record CreateUnitRequest(string Identifier, string DisplayName, Guid? BlockId);
public sealed record CreateResidentRequest(string FullName, string PhoneE164, Guid UnitId, ResidentUnitRole Role);

public static class CoreOperationsEndpoints
{
    public static IEndpointRouteBuilder MapSentraCoreOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1");

        group.MapGet("/units", async (ClaimsPrincipal user, SentraDbContext db, CancellationToken cancellationToken) =>
        {
            if (!TryGetCondominiumId(user, out var condominiumId)) return Results.Unauthorized();

            var units = await db.Units
                .AsNoTracking()
                .Where(unit => unit.CondominiumId == condominiumId && unit.IsActive)
                .OrderBy(unit => unit.Identifier)
                .Select(unit => new
                {
                    unit.Id,
                    unit.Identifier,
                    unit.DisplayName,
                    unit.BlockId
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(units);
        }).RequireAuthorization(PermissionCatalog.UnitsRead);

        group.MapPost("/blocks", async (
            CreateBlockRequest request,
            ClaimsPrincipal user,
            SentraDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetCondominiumId(user, out var condominiumId)) return Results.Unauthorized();

            var block = new Block(condominiumId, request.Name);
            db.Blocks.Add(block);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Já existe um bloco com este nome ou os dados são conflitantes." });
            }

            return Results.Created($"/api/v1/blocks/{block.Id}", new { block.Id, block.Name });
        }).RequireAuthorization(PermissionCatalog.UnitsManage);

        group.MapPost("/units", async (
            CreateUnitRequest request,
            ClaimsPrincipal user,
            SentraDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetCondominiumId(user, out var condominiumId)) return Results.Unauthorized();

            if (request.BlockId is not null)
            {
                var blockExists = await db.Blocks.AnyAsync(
                    block => block.Id == request.BlockId && block.CondominiumId == condominiumId && block.IsActive,
                    cancellationToken);

                if (!blockExists) return Results.BadRequest(new { error = "Bloco não pertence ao condomínio." });
            }

            var unit = new Unit(condominiumId, request.Identifier, request.DisplayName, request.BlockId);
            db.Units.Add(unit);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Já existe uma unidade com este identificador." });
            }

            return Results.Created($"/api/v1/units/{unit.Id}", new
            {
                unit.Id,
                unit.Identifier,
                unit.DisplayName,
                unit.BlockId
            });
        }).RequireAuthorization(PermissionCatalog.UnitsManage);

        group.MapGet("/residents", async (
            ClaimsPrincipal user,
            SentraDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetCondominiumId(user, out var condominiumId)) return Results.Unauthorized();

            var residents = await (
                from resident in db.Residents.AsNoTracking()
                join link in db.ResidentUnits.AsNoTracking() on resident.Id equals link.ResidentId
                join unit in db.Units.AsNoTracking() on link.UnitId equals unit.Id
                where unit.CondominiumId == condominiumId && resident.IsActive
                orderby resident.FullName
                select new
                {
                    resident.Id,
                    resident.FullName,
                    unitId = unit.Id,
                    unit = unit.DisplayName,
                    link.Role,
                    link.IsPrimary
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(residents);
        }).RequireAuthorization(PermissionCatalog.ResidentsRead);

        group.MapPost("/residents", async (
            CreateResidentRequest request,
            ClaimsPrincipal user,
            SentraDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetCondominiumId(user, out var condominiumId)) return Results.Unauthorized();

            var unitExists = await db.Units.AnyAsync(
                unit => unit.Id == request.UnitId && unit.CondominiumId == condominiumId && unit.IsActive,
                cancellationToken);

            if (!unitExists) return Results.BadRequest(new { error = "Unidade não pertence ao condomínio." });

            string e164;
            try
            {
                e164 = ResidentPhone.NormalizeE164(request.PhoneE164);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }

            if (await db.ResidentPhones.AnyAsync(phone => phone.E164 == e164, cancellationToken))
            {
                return Results.Conflict(new { error = "Este telefone já está associado a um morador." });
            }

            var resident = new Resident(request.FullName);
            var phone = new ResidentPhone(resident.Id, e164);
            var link = new ResidentUnit(resident.Id, request.UnitId, request.Role);

            db.Residents.Add(resident);
            db.ResidentPhones.Add(phone);
            db.ResidentUnits.Add(link);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/v1/residents/{resident.Id}", new
            {
                resident.Id,
                resident.FullName,
                phone = phone.E164,
                unitId = request.UnitId,
                role = request.Role
            });
        }).RequireAuthorization(PermissionCatalog.ResidentsManage);

        return endpoints;
    }

    private static bool TryGetCondominiumId(ClaimsPrincipal user, out Guid condominiumId)
    {
        var claim = user.FindFirst("condominium_id")?.Value;
        return Guid.TryParse(claim, out condominiumId);
    }
}

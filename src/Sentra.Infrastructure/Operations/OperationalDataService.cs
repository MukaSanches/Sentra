using Microsoft.EntityFrameworkCore;
using Sentra.Application.Abstractions;
using Sentra.Application.Operations;
using Sentra.Contracts.Common;
using Sentra.Contracts.Operations;
using Sentra.Domain.Access;
using Sentra.Domain.Auditing;
using Sentra.Domain.Properties;
using Sentra.Domain.Residents;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Infrastructure.Operations;

public sealed class OperationalDataService(
    SentraDbContext dbContext,
    IClock clock) : IOperationalDataService
{
    public async Task<SetupStatusResponse> GetSetupStatusAsync(CancellationToken cancellationToken)
        => new(await dbContext.Condominiums.AsNoTracking().AnyAsync(cancellationToken));

    public async Task<CondominiumResponse> BootstrapAsync(
        BootstrapRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Condominiums.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("SENTRA já foi inicializado.");
        }

        var now = clock.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var condominium = new Condominium(request.CondominiumName, actorSubject, now);
        dbContext.Condominiums.Add(condominium);

        var employee = new Employee(condominium.Id, actorSubject, request.AdministratorName, actorSubject, now);
        dbContext.Employees.Add(employee);

        var ownerRole = new Role(condominium.Id, "Proprietário", actorSubject, isSystem: true, now);
        dbContext.Roles.Add(ownerRole);
        dbContext.EmployeeRoles.Add(new EmployeeRole(condominium.Id, employee.Id, ownerRole.Id, actorSubject, now));

        var permissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        if (permissions.Count == 0)
        {
            throw new InvalidOperationException("Permissões do sistema não foram carregadas.");
        }

        foreach (var permission in permissions)
        {
            dbContext.RolePermissions.Add(
                new RolePermission(condominium.Id, ownerRole.Id, permission.Id, actorSubject, now));
        }

        AddAudit(
            "system.bootstrap",
            nameof(Condominium),
            condominium.Id,
            actorSubject,
            correlationId,
            now);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToResponse(condominium);
    }

    public async Task<CondominiumResponse?> GetCondominiumAsync(
        Guid condominiumId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Condominiums.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == condominiumId, cancellationToken);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<CondominiumResponse?> UpdateCondominiumAsync(
        Guid condominiumId,
        UpdateCondominiumRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Condominiums.SingleOrDefaultAsync(x => x.Id == condominiumId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var now = clock.UtcNow;
        entity.Rename(request.Name, actorSubject, now);
        entity.SetActive(request.IsActive, actorSubject, now);
        AddAudit("condominium.updated", nameof(Condominium), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<PagedResponse<BlockResponse>> ListBlocksAsync(
        Guid condominiumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = dbContext.Blocks.AsNoTracking().Where(x => x.CondominiumId == condominiumId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BlockResponse(x.Id, x.CondominiumId, x.Name, x.Code, x.IsActive))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }

    public async Task<BlockResponse> CreateBlockAsync(
        Guid condominiumId,
        CreateBlockRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        await EnsureCondominiumExists(condominiumId, cancellationToken);
        var now = clock.UtcNow;
        var entity = new Block(condominiumId, request.Name, actorSubject, request.Code, now);
        dbContext.Blocks.Add(entity);
        AddAudit("block.created", nameof(Block), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<BlockResponse?> UpdateBlockAsync(
        Guid condominiumId,
        Guid blockId,
        UpdateBlockRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Blocks.SingleOrDefaultAsync(
            x => x.Id == blockId && x.CondominiumId == condominiumId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var now = clock.UtcNow;
        entity.Update(request.Name, request.Code, actorSubject, now);
        entity.SetActive(request.IsActive, actorSubject, now);
        AddAudit("block.updated", nameof(Block), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<PagedResponse<UnitResponse>> ListUnitsAsync(
        Guid condominiumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = dbContext.Units.AsNoTracking().Where(x => x.CondominiumId == condominiumId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.BlockId).ThenBy(x => x.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UnitResponse(x.Id, x.CondominiumId, x.BlockId, x.Number, x.Floor, x.IsActive))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }

    public async Task<UnitResponse> CreateUnitAsync(
        Guid condominiumId,
        CreateUnitRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var blockExists = await dbContext.Blocks.AnyAsync(
            x => x.Id == request.BlockId && x.CondominiumId == condominiumId,
            cancellationToken);
        if (!blockExists)
        {
            throw new ArgumentException("Bloco não pertence ao condomínio.", nameof(request));
        }

        var now = clock.UtcNow;
        var entity = new Unit(condominiumId, request.BlockId, request.Number, actorSubject, request.Floor, now);
        dbContext.Units.Add(entity);
        AddAudit("unit.created", nameof(Unit), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<UnitResponse?> UpdateUnitAsync(
        Guid condominiumId,
        Guid unitId,
        UpdateUnitRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Units.SingleOrDefaultAsync(
            x => x.Id == unitId && x.CondominiumId == condominiumId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var now = clock.UtcNow;
        entity.Update(request.Number, request.Floor, actorSubject, now);
        entity.SetActive(request.IsActive, actorSubject, now);
        AddAudit("unit.updated", nameof(Unit), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<PagedResponse<ResidentResponse>> ListResidentsAsync(
        Guid condominiumId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = dbContext.Residents.AsNoTracking().Where(x => x.CondominiumId == condominiumId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.FullName, $"%{term}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ResidentResponse(x.Id, x.CondominiumId, x.FullName, x.IsActive))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }

    public async Task<ResidentResponse> CreateResidentAsync(
        Guid condominiumId,
        CreateResidentRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        await EnsureCondominiumExists(condominiumId, cancellationToken);
        var now = clock.UtcNow;
        var entity = new Resident(condominiumId, request.FullName, actorSubject, now);
        dbContext.Residents.Add(entity);
        AddAudit("resident.created", nameof(Resident), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<ResidentResponse?> UpdateResidentAsync(
        Guid condominiumId,
        Guid residentId,
        UpdateResidentRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Residents.SingleOrDefaultAsync(
            x => x.Id == residentId && x.CondominiumId == condominiumId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var now = clock.UtcNow;
        entity.Rename(request.FullName, actorSubject, now);
        entity.SetActive(request.IsActive, actorSubject, now);
        AddAudit("resident.updated", nameof(Resident), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<ResidentPhoneResponse?> AddResidentPhoneAsync(
        Guid condominiumId,
        Guid residentId,
        AddResidentPhoneRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var residentExists = await dbContext.Residents.AnyAsync(
            x => x.Id == residentId && x.CondominiumId == condominiumId,
            cancellationToken);
        if (!residentExists)
        {
            return null;
        }

        var now = clock.UtcNow;
        var entity = new ResidentPhone(
            condominiumId,
            residentId,
            request.E164Number,
            request.IsPrimary,
            request.IsWhatsAppEnabled,
            actorSubject,
            now);
        dbContext.ResidentPhones.Add(entity);
        AddAudit("resident.phone.created", nameof(ResidentPhone), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.Id, entity.ResidentId, entity.E164Number, entity.IsPrimary, entity.IsWhatsAppEnabled);
    }

    public async Task<ResidentUnitResponse?> LinkResidentUnitAsync(
        Guid condominiumId,
        Guid residentId,
        LinkResidentUnitRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var residentExists = await dbContext.Residents.AnyAsync(
            x => x.Id == residentId && x.CondominiumId == condominiumId,
            cancellationToken);
        var unitExists = await dbContext.Units.AnyAsync(
            x => x.Id == request.UnitId && x.CondominiumId == condominiumId,
            cancellationToken);
        if (!residentExists || !unitExists)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(ResidentUnitRole), request.Role))
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Função do morador na unidade é inválida.");
        }

        var role = (ResidentUnitRole)request.Role;
        var entity = new ResidentUnit(
            condominiumId,
            residentId,
            request.UnitId,
            role,
            request.IsPrimary,
            actorSubject,
            request.StartsAt);
        dbContext.ResidentUnits.Add(entity);
        AddAudit("resident.unit.linked", nameof(ResidentUnit), entity.Id, actorSubject, correlationId, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.Id, entity.ResidentId, entity.UnitId, (int)entity.Role, entity.IsPrimary, entity.StartsAt, entity.EndsAt);
    }

    public async Task<PagedResponse<EmployeeResponse>> ListEmployeesAsync(
        Guid condominiumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var query = dbContext.Employees.AsNoTracking().Where(x => x.CondominiumId == condominiumId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmployeeResponse(x.Id, x.CondominiumId, x.FullName, x.IdentitySubject, x.IsActive))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<RoleResponse>> ListRolesAsync(
        Guid condominiumId,
        CancellationToken cancellationToken)
        => await dbContext.Roles.AsNoTracking()
            .Where(x => x.CondominiumId == condominiumId)
            .OrderBy(x => x.Name)
            .Select(x => new RoleResponse(x.Id, x.CondominiumId, x.Name, x.IsSystem))
            .ToListAsync(cancellationToken);

    public async Task<RoleResponse> CreateRoleAsync(
        Guid condominiumId,
        CreateRoleRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        await EnsureCondominiumExists(condominiumId, cancellationToken);
        var now = clock.UtcNow;
        var entity = new Role(condominiumId, request.Name, actorSubject, isSystem: false, now);
        dbContext.Roles.Add(entity);
        AddAudit("role.created", nameof(Role), entity.Id, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.Id, entity.CondominiumId, entity.Name, entity.IsSystem);
    }

    public async Task<bool> SetRolePermissionsAsync(
        Guid condominiumId,
        Guid roleId,
        SetRolePermissionsRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(
            x => x.Id == roleId && x.CondominiumId == condominiumId,
            cancellationToken);
        if (role is null)
        {
            return false;
        }

        var requestedCodes = request.PermissionCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (requestedCodes.Any(code => !PermissionCodes.All.Contains(code)))
        {
            throw new ArgumentException("A lista contém uma permissão desconhecida.", nameof(request));
        }

        var requestedPermissions = await dbContext.Permissions
            .Where(x => requestedCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        var current = await dbContext.RolePermissions
            .Where(x => x.CondominiumId == condominiumId && x.RoleId == roleId)
            .ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(current);

        var now = clock.UtcNow;
        foreach (var permission in requestedPermissions)
        {
            dbContext.RolePermissions.Add(
                new RolePermission(condominiumId, roleId, permission.Id, actorSubject, now));
        }

        AddAudit("role.permissions.updated", nameof(Role), roleId, actorSubject, correlationId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AssignEmployeeRoleAsync(
        Guid condominiumId,
        Guid employeeId,
        AssignEmployeeRoleRequest request,
        string actorSubject,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(
            x => x.Id == employeeId && x.CondominiumId == condominiumId,
            cancellationToken);
        var roleExists = await dbContext.Roles.AnyAsync(
            x => x.Id == request.RoleId && x.CondominiumId == condominiumId,
            cancellationToken);
        if (!employeeExists || !roleExists)
        {
            return false;
        }

        var exists = await dbContext.EmployeeRoles.AnyAsync(
            x => x.EmployeeId == employeeId && x.RoleId == request.RoleId,
            cancellationToken);
        if (!exists)
        {
            var now = clock.UtcNow;
            dbContext.EmployeeRoles.Add(
                new EmployeeRole(condominiumId, employeeId, request.RoleId, actorSubject, now));
            AddAudit("employee.role.assigned", nameof(Employee), employeeId, actorSubject, correlationId, now);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private async Task EnsureCondominiumExists(Guid condominiumId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Condominiums.AnyAsync(x => x.Id == condominiumId, cancellationToken))
        {
            throw new ArgumentException("Condomínio não encontrado.", nameof(condominiumId));
        }
    }

    private void AddAudit(
        string action,
        string entityType,
        Guid entityId,
        string actorSubject,
        string? correlationId,
        DateTimeOffset occurredAt)
        => dbContext.AuditEvents.Add(
            new AuditEvent(action, entityType, entityId.ToString(), "success", occurredAt, actorSubject, correlationId));

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize)
        => (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));

    private static CondominiumResponse ToResponse(Condominium entity)
        => new(entity.Id, entity.Name, entity.IsActive);

    private static BlockResponse ToResponse(Block entity)
        => new(entity.Id, entity.CondominiumId, entity.Name, entity.Code, entity.IsActive);

    private static UnitResponse ToResponse(Unit entity)
        => new(entity.Id, entity.CondominiumId, entity.BlockId, entity.Number, entity.Floor, entity.IsActive);

    private static ResidentResponse ToResponse(Resident entity)
        => new(entity.Id, entity.CondominiumId, entity.FullName, entity.IsActive);
}

using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Operations;
using Sentra.Contracts.Operations;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Infrastructure.Operations;

public sealed class OperationalStore(SentraDbContext db) : IOperationalStore
{
    public async Task<IReadOnlyList<VisitorAuthorizationResponse>> ListVisitorAuthorizationsAsync(
        Guid condominiumId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, unit_id, resident_id, visitor_name, document, relationship,
       starts_at, ends_at, vehicle_plate, purpose, status, checked_in_at, checked_out_at
FROM visitor_authorizations
WHERE condominium_id = @condominium_id
  AND (@active_only = FALSE OR (status IN ('Authorized','Inside') AND ends_at >= now()))
ORDER BY starts_at DESC
LIMIT 500;
""";
        return await QueryAsync(sql, cmd =>
        {
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@active_only", activeOnly);
        }, MapVisitorAuthorization, cancellationToken);
    }

    public async Task<VisitorAuthorizationResponse> CreateVisitorAuthorizationAsync(
        Guid condominiumId,
        CreateVisitorAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        ValidateWindow(request.StartsAt, request.EndsAt);

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var plate = NormalizePlate(request.VehiclePlate);

        const string sql = """
INSERT INTO visitor_authorizations
(id, condominium_id, unit_id, resident_id, visitor_name, document, relationship,
 starts_at, ends_at, vehicle_plate, purpose, status, created_at, updated_at)
SELECT @id, @condominium_id, @unit_id, @resident_id, @visitor_name, @document, @relationship,
       @starts_at, @ends_at, @vehicle_plate, @purpose, 'Authorized', @now, @now
WHERE EXISTS (
    SELECT 1 FROM units
    WHERE id = @unit_id AND condominium_id = @condominium_id AND is_active = TRUE
)
RETURNING id, unit_id, resident_id, visitor_name, document, relationship,
          starts_at, ends_at, vehicle_plate, purpose, status, checked_in_at, checked_out_at;
""";
        var result = await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@id", id);
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@unit_id", request.UnitId);
            Add(cmd, "@resident_id", request.ResidentId);
            Add(cmd, "@visitor_name", Required(request.VisitorName, 160));
            Add(cmd, "@document", Optional(request.Document, 64));
            Add(cmd, "@relationship", Optional(request.Relationship, 64));
            Add(cmd, "@starts_at", request.StartsAt);
            Add(cmd, "@ends_at", request.EndsAt);
            Add(cmd, "@vehicle_plate", plate);
            Add(cmd, "@purpose", Optional(request.Purpose, 256));
            Add(cmd, "@now", now);
        }, MapVisitorAuthorization, cancellationToken);

        return result ?? throw new InvalidOperationException("A unidade não pertence ao condomínio.");
    }

    public Task<VisitorAuthorizationResponse?> GetVisitorAuthorizationAsync(
        Guid condominiumId,
        Guid authorizationId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, unit_id, resident_id, visitor_name, document, relationship,
       starts_at, ends_at, vehicle_plate, purpose, status, checked_in_at, checked_out_at
FROM visitor_authorizations
WHERE condominium_id = @condominium_id AND id = @id;
""";
        return QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@id", authorizationId);
        }, MapVisitorAuthorization, cancellationToken);
    }

    public async Task<VisitorAuthorizationResponse?> RegisterVisitAsync(
        Guid condominiumId,
        Guid authorizationId,
        Guid actorId,
        string action,
        CancellationToken cancellationToken)
    {
        var normalized = action.Trim().ToLowerInvariant();
        if (normalized is not ("entry" or "exit"))
        {
            throw new ArgumentException("Ação deve ser entry ou exit.", nameof(action));
        }

        var now = DateTimeOffset.UtcNow;
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var connection = db.Database.GetDbConnection();
            await using var update = connection.CreateCommand();
            update.Transaction = transaction.GetDbTransaction();
            update.CommandText = normalized == "entry"
                ? """
UPDATE visitor_authorizations
SET status = 'Inside', checked_in_at = COALESCE(checked_in_at, @now), updated_at = @now
WHERE condominium_id = @condominium_id
  AND id = @id
  AND status = 'Authorized'
  AND starts_at <= @now
  AND ends_at >= @now
RETURNING id, unit_id, resident_id, visitor_name, document, relationship,
          starts_at, ends_at, vehicle_plate, purpose, status, checked_in_at, checked_out_at;
"""
                : """
UPDATE visitor_authorizations
SET status = 'Completed', checked_out_at = @now, updated_at = @now
WHERE condominium_id = @condominium_id
  AND id = @id
  AND status = 'Inside'
RETURNING id, unit_id, resident_id, visitor_name, document, relationship,
          starts_at, ends_at, vehicle_plate, purpose, status, checked_in_at, checked_out_at;
""";
            Add(update, "@now", now);
            Add(update, "@condominium_id", condominiumId);
            Add(update, "@id", authorizationId);

            VisitorAuthorizationResponse? result;
            await using (var reader = await update.ExecuteReaderAsync(cancellationToken))
            {
                result = await reader.ReadAsync(cancellationToken)
                    ? MapVisitorAuthorization(reader)
                    : null;
            }

            if (result is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction.GetDbTransaction();
            insert.CommandText = """
INSERT INTO visits(id, condominium_id, authorization_id, action, occurred_at, actor_id)
VALUES(@visit_id, @condominium_id, @authorization_id, @action, @now, @actor_id);
""";
            Add(insert, "@visit_id", Guid.NewGuid());
            Add(insert, "@condominium_id", condominiumId);
            Add(insert, "@authorization_id", authorizationId);
            Add(insert, "@action", normalized == "entry" ? "Entry" : "Exit");
            Add(insert, "@now", now);
            Add(insert, "@actor_id", actorId);
            await insert.ExecuteNonQueryAsync(cancellationToken);

            if (normalized == "entry")
            {
                await using var consume = connection.CreateCommand();
                consume.Transaction = transaction.GetDbTransaction();
                consume.CommandText = """
UPDATE qr_credentials
SET used_at = @now
WHERE condominium_id = @condominium_id
  AND authorization_id = @authorization_id
  AND one_time = TRUE
  AND used_at IS NULL
  AND revoked_at IS NULL;
""";
                Add(consume, "@now", now);
                Add(consume, "@condominium_id", condominiumId);
                Add(consume, "@authorization_id", authorizationId);
                await consume.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task<(Guid Id, string Payload, DateTimeOffset ExpiresAt, bool OneTime)> CreateQrCredentialAsync(
        Guid condominiumId,
        Guid authorizationId,
        bool oneTime,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var authorization = await GetVisitorAuthorizationAsync(condominiumId, authorizationId, cancellationToken)
            ?? throw new InvalidOperationException("Autorização não encontrada.");

        if (expiresAt > authorization.EndsAt)
        {
            expiresAt = authorization.EndsAt;
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("A validade do QR precisa estar no futuro.");
        }

        var id = Guid.NewGuid();
        var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var payload = $"SENTRA:V1:{authorizationId:N}:{random}";
        var hash = Hash(payload);

        const string sql = """
INSERT INTO qr_credentials
(id, condominium_id, authorization_id, token_hash, expires_at, one_time, created_at)
VALUES(@id, @condominium_id, @authorization_id, @token_hash, @expires_at, @one_time, @now);
""";
        await ExecuteAsync(sql, cmd =>
        {
            Add(cmd, "@id", id);
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@authorization_id", authorizationId);
            Add(cmd, "@token_hash", hash);
            Add(cmd, "@expires_at", expiresAt);
            Add(cmd, "@one_time", oneTime);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
        }, cancellationToken);

        return (id, payload, expiresAt, oneTime);
    }

    public async Task<VisitorAuthorizationResponse?> ValidateQrAsync(
        Guid condominiumId,
        string payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload) || payload.Length > 512)
        {
            return null;
        }

        const string sql = """
SELECT a.id, a.unit_id, a.resident_id, a.visitor_name, a.document, a.relationship,
       a.starts_at, a.ends_at, a.vehicle_plate, a.purpose, a.status, a.checked_in_at, a.checked_out_at
FROM qr_credentials q
JOIN visitor_authorizations a ON a.id = q.authorization_id
WHERE q.condominium_id = @condominium_id
  AND q.token_hash = @token_hash
  AND q.revoked_at IS NULL
  AND q.expires_at >= now()
  AND (q.one_time = FALSE OR q.used_at IS NULL)
  AND a.status IN ('Authorized','Inside')
  AND a.ends_at >= now()
LIMIT 1;
""";
        return await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@token_hash", Hash(payload.Trim()));
        }, MapVisitorAuthorization, cancellationToken);
    }

    public Task<IReadOnlyList<ServiceProviderResponse>> ListProvidersAsync(
        Guid condominiumId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, name, document, company, active
FROM service_providers
WHERE condominium_id = @condominium_id
ORDER BY name;
""";
        return QueryAsync(sql, cmd => Add(cmd, "@condominium_id", condominiumId),
            r => new ServiceProviderResponse(
                r.GetGuid(0), r.GetString(1), NullableString(r, 2), NullableString(r, 3), r.GetBoolean(4)),
            cancellationToken);
    }

    public async Task<ServiceProviderResponse> CreateProviderAsync(
        Guid condominiumId,
        CreateServiceProviderRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
INSERT INTO service_providers
(id, condominium_id, name, document, company, active, created_at, updated_at)
VALUES(@id, @condominium_id, @name, @document, @company, TRUE, @now, @now)
RETURNING id, name, document, company, active;
""";
        return (await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@id", Guid.NewGuid());
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@name", Required(request.Name, 160));
            Add(cmd, "@document", Optional(request.Document, 64));
            Add(cmd, "@company", Optional(request.Company, 160));
            Add(cmd, "@now", DateTimeOffset.UtcNow);
        }, r => new ServiceProviderResponse(
            r.GetGuid(0), r.GetString(1), NullableString(r, 2), NullableString(r, 3), r.GetBoolean(4)),
            cancellationToken))!;
    }

    public async Task<ProviderAuthorizationResponse> CreateProviderAuthorizationAsync(
        Guid condominiumId,
        CreateProviderAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        ValidateWindow(request.StartsAt, request.EndsAt);
        const string sql = """
INSERT INTO provider_authorizations
(id, condominium_id, provider_id, unit_id, starts_at, ends_at, purpose, status, created_at, updated_at)
SELECT @id, @condominium_id, @provider_id, @unit_id, @starts_at, @ends_at, @purpose,
       'Authorized', @now, @now
WHERE EXISTS (
    SELECT 1 FROM service_providers
    WHERE id = @provider_id AND condominium_id = @condominium_id AND active = TRUE
)
RETURNING id, provider_id, unit_id, starts_at, ends_at, purpose, status;
""";
        var result = await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@id", Guid.NewGuid());
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@provider_id", request.ProviderId);
            Add(cmd, "@unit_id", request.UnitId);
            Add(cmd, "@starts_at", request.StartsAt);
            Add(cmd, "@ends_at", request.EndsAt);
            Add(cmd, "@purpose", Required(request.Purpose, 256));
            Add(cmd, "@now", DateTimeOffset.UtcNow);
        }, r => new ProviderAuthorizationResponse(
            r.GetGuid(0), r.GetGuid(1), NullableGuid(r, 2),
            ReadTimestamp(r, 3), ReadTimestamp(r, 4), r.GetString(5), r.GetString(6)),
            cancellationToken);

        return result ?? throw new InvalidOperationException("Prestador não encontrado.");
    }

    public Task<IReadOnlyList<PackageResponse>> ListPackagesAsync(
        Guid condominiumId,
        bool pendingOnly,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, unit_id, carrier, description, received_at, status, picked_up_at
FROM packages
WHERE condominium_id = @condominium_id
  AND (@pending_only = FALSE OR status = 'Waiting')
ORDER BY received_at DESC
LIMIT 500;
""";
        return QueryAsync(sql, cmd =>
        {
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@pending_only", pendingOnly);
        }, MapPackage, cancellationToken);
    }

    public async Task<PackageResponse> CreatePackageAsync(
        Guid condominiumId,
        Guid actorId,
        CreatePackageRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
INSERT INTO packages
(id, condominium_id, unit_id, carrier, description, received_at, received_by, status, created_at, updated_at)
SELECT @id, @condominium_id, @unit_id, @carrier, @description, @now, @actor_id, 'Waiting', @now, @now
WHERE EXISTS (
    SELECT 1 FROM units
    WHERE id = @unit_id AND condominium_id = @condominium_id AND is_active = TRUE
)
RETURNING id, unit_id, carrier, description, received_at, status, picked_up_at;
""";
        var result = await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@id", Guid.NewGuid());
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@unit_id", request.UnitId);
            Add(cmd, "@carrier", Required(request.Carrier, 120));
            Add(cmd, "@description", Optional(request.Description, 512));
            Add(cmd, "@now", DateTimeOffset.UtcNow);
            Add(cmd, "@actor_id", actorId);
        }, MapPackage, cancellationToken);

        return result ?? throw new InvalidOperationException("Unidade não encontrada.");
    }

    public Task<PackageResponse?> CollectPackageAsync(
        Guid condominiumId,
        Guid packageId,
        Guid actorId,
        string? pickupCode,
        CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE packages
SET status = 'Collected', picked_up_at = @now, picked_up_by = @actor_id, updated_at = @now
WHERE condominium_id = @condominium_id
  AND id = @id
  AND status = 'Waiting'
RETURNING id, unit_id, carrier, description, received_at, status, picked_up_at;
""";
        return QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@now", DateTimeOffset.UtcNow);
            Add(cmd, "@actor_id", actorId);
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@id", packageId);
        }, MapPackage, cancellationToken);
    }

    public Task<IReadOnlyList<OccurrenceResponse>> ListOccurrencesAsync(
        Guid condominiumId,
        bool openOnly,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, category, title, description, priority, status, unit_id, owner_employee_id, created_at, updated_at
FROM occurrences
WHERE condominium_id = @condominium_id
  AND (@open_only = FALSE OR status NOT IN ('Resolved','Cancelled'))
ORDER BY created_at DESC
LIMIT 500;
""";
        return QueryAsync(sql, cmd =>
        {
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@open_only", openOnly);
        }, MapOccurrence, cancellationToken);
    }

    public async Task<OccurrenceResponse> CreateOccurrenceAsync(
        Guid condominiumId,
        CreateOccurrenceRequest request,
        CancellationToken cancellationToken)
    {
        var priority = NormalizeChoice(request.Priority, ["Low", "Normal", "High", "Critical"], "Normal");
        var category = Required(request.Category, 64);
        const string sql = """
INSERT INTO occurrences
(id, condominium_id, category, title, description, priority, status, unit_id, created_at, updated_at)
VALUES(@id, @condominium_id, @category, @title, @description, @priority, 'Open', @unit_id, @now, @now)
RETURNING id, category, title, description, priority, status, unit_id, owner_employee_id, created_at, updated_at;
""";
        return (await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@id", Guid.NewGuid());
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@category", category);
            Add(cmd, "@title", Required(request.Title, 180));
            Add(cmd, "@description", Required(request.Description, 8000));
            Add(cmd, "@priority", priority);
            Add(cmd, "@unit_id", request.UnitId);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
        }, MapOccurrence, cancellationToken))!;
    }

    public Task<OccurrenceResponse?> UpdateOccurrenceAsync(
        Guid condominiumId,
        Guid occurrenceId,
        UpdateOccurrenceRequest request,
        CancellationToken cancellationToken)
    {
        var status = NormalizeChoice(request.Status, ["Open", "InProgress", "Resolved", "Cancelled"], "Open");
        const string sql = """
UPDATE occurrences
SET status = @status, owner_employee_id = @owner_employee_id, updated_at = @now
WHERE condominium_id = @condominium_id AND id = @id
RETURNING id, category, title, description, priority, status, unit_id, owner_employee_id, created_at, updated_at;
""";
        return QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@status", status);
            Add(cmd, "@owner_employee_id", request.OwnerEmployeeId);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@id", occurrenceId);
        }, MapOccurrence, cancellationToken);
    }

    public Task<IReadOnlyList<ShiftResponse>> ListShiftsAsync(
        Guid condominiumId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, employee_id, opened_at, closed_at, handoff_summary, acknowledged_by, acknowledged_at
FROM shifts
WHERE condominium_id = @condominium_id
ORDER BY opened_at DESC
LIMIT 100;
""";
        return QueryAsync(sql, cmd => Add(cmd, "@condominium_id", condominiumId), MapShift, cancellationToken);
    }

    public async Task<ShiftResponse> OpenShiftAsync(
        Guid condominiumId,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var existing = await QuerySingleAsync("""
SELECT id, employee_id, opened_at, closed_at, handoff_summary, acknowledged_by, acknowledged_at
FROM shifts
WHERE condominium_id = @condominium_id AND employee_id = @employee_id AND closed_at IS NULL
ORDER BY opened_at DESC LIMIT 1;
""", cmd =>
        {
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@employee_id", employeeId);
        }, MapShift, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        const string sql = """
INSERT INTO shifts
(id, condominium_id, employee_id, opened_at, created_at, updated_at)
VALUES(@id, @condominium_id, @employee_id, @now, @now, @now)
RETURNING id, employee_id, opened_at, closed_at, handoff_summary, acknowledged_by, acknowledged_at;
""";
        return (await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@id", Guid.NewGuid());
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@employee_id", employeeId);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
        }, MapShift, cancellationToken))!;
    }

    public Task<ShiftResponse?> CloseShiftAsync(
        Guid condominiumId,
        Guid shiftId,
        Guid employeeId,
        string summary,
        CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE shifts
SET closed_at = @now, handoff_summary = @summary, updated_at = @now
WHERE condominium_id = @condominium_id
  AND id = @id
  AND employee_id = @employee_id
  AND closed_at IS NULL
RETURNING id, employee_id, opened_at, closed_at, handoff_summary, acknowledged_by, acknowledged_at;
""";
        return QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@now", DateTimeOffset.UtcNow);
            Add(cmd, "@summary", Required(summary, 8000));
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@id", shiftId);
            Add(cmd, "@employee_id", employeeId);
        }, MapShift, cancellationToken);
    }

    public Task<ShiftResponse?> AcknowledgeShiftAsync(
        Guid condominiumId,
        Guid shiftId,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE shifts
SET acknowledged_by = @employee_id, acknowledged_at = @now, updated_at = @now
WHERE condominium_id = @condominium_id
  AND id = @id
  AND closed_at IS NOT NULL
  AND acknowledged_at IS NULL
RETURNING id, employee_id, opened_at, closed_at, handoff_summary, acknowledged_by, acknowledged_at;
""";
        return QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@employee_id", employeeId);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@id", shiftId);
        }, MapShift, cancellationToken);
    }

    public Task<IReadOnlyList<AnnouncementResponse>> ListAnnouncementsAsync(
        Guid condominiumId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, title, body, audience, starts_at, ends_at, active
FROM announcements
WHERE condominium_id = @condominium_id
ORDER BY starts_at DESC
LIMIT 200;
""";
        return QueryAsync(sql, cmd => Add(cmd, "@condominium_id", condominiumId),
            r => new AnnouncementResponse(
                r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetString(3),
                ReadTimestamp(r, 4), NullableTimestamp(r, 5), r.GetBoolean(6)),
            cancellationToken);
    }

    public async Task<AnnouncementResponse> CreateAnnouncementAsync(
        Guid condominiumId,
        CreateAnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EndsAt is not null && request.EndsAt <= request.StartsAt)
        {
            throw new ArgumentException("Fim do comunicado deve ser posterior ao início.");
        }

        const string sql = """
INSERT INTO announcements
(id, condominium_id, title, body, audience, starts_at, ends_at, active, created_at, updated_at)
VALUES(@id, @condominium_id, @title, @body, @audience, @starts_at, @ends_at, TRUE, @now, @now)
RETURNING id, title, body, audience, starts_at, ends_at, active;
""";
        return (await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@id", Guid.NewGuid());
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@title", Required(request.Title, 180));
            Add(cmd, "@body", Required(request.Body, 8000));
            Add(cmd, "@audience", Required(request.Audience, 80));
            Add(cmd, "@starts_at", request.StartsAt);
            Add(cmd, "@ends_at", request.EndsAt);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
        }, r => new AnnouncementResponse(
            r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetString(3),
            ReadTimestamp(r, 4), NullableTimestamp(r, 5), r.GetBoolean(6)),
            cancellationToken))!;
    }

    public async Task<Guid> SaveInterpretationAsync(
        Guid condominiumId,
        Guid conversationId,
        string intent,
        string resolutionState,
        string summary,
        string extractedJson,
        string missingJson,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        const string sql = """
INSERT INTO ai_interpretations
(id, condominium_id, conversation_id, intent, resolution_state, summary, extracted_json, missing_json, created_at)
VALUES(@id, @condominium_id, @conversation_id, @intent, @resolution_state, @summary,
       CAST(@extracted_json AS jsonb), CAST(@missing_json AS jsonb), @now);
""";
        await ExecuteAsync(sql, cmd =>
        {
            Add(cmd, "@id", id);
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@conversation_id", conversationId);
            Add(cmd, "@intent", Required(intent, 80));
            Add(cmd, "@resolution_state", Required(resolutionState, 48));
            Add(cmd, "@summary", Required(summary, 1000));
            Add(cmd, "@extracted_json", extractedJson);
            Add(cmd, "@missing_json", missingJson);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
        }, cancellationToken);
        return id;
    }

    public async Task<Guid> CreatePendingActionAsync(
        Guid condominiumId,
        Guid conversationId,
        Guid interpretationId,
        string actionType,
        string riskLevel,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        const string sql = """
INSERT INTO pending_actions
(id, condominium_id, conversation_id, interpretation_id, action_type, risk_level, payload_json, status, created_at)
VALUES(@id, @condominium_id, @conversation_id, @interpretation_id, @action_type, @risk_level,
       CAST(@payload_json AS jsonb), 'Pending', @now);
""";
        await ExecuteAsync(sql, cmd =>
        {
            Add(cmd, "@id", id);
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@conversation_id", conversationId);
            Add(cmd, "@interpretation_id", interpretationId);
            Add(cmd, "@action_type", Required(actionType, 96));
            Add(cmd, "@risk_level", Required(riskLevel, 48));
            Add(cmd, "@payload_json", payloadJson);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
        }, cancellationToken);
        return id;
    }

    public Task<PendingActionResponse?> GetPendingActionAsync(
        Guid condominiumId,
        Guid pendingActionId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, conversation_id, action_type, risk_level, status, payload_json::text, created_at
FROM pending_actions
WHERE condominium_id = @condominium_id AND id = @id;
""";
        return QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@id", pendingActionId);
        }, r => new PendingActionResponse(
            r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), ReadTimestamp(r, 6)),
            cancellationToken);
    }

    public Task<PendingActionResponse?> ResolvePendingActionAsync(
        Guid condominiumId,
        Guid pendingActionId,
        Guid employeeId,
        string decision,
        CancellationToken cancellationToken)
    {
        var status = decision.Trim().ToLowerInvariant() switch
        {
            "confirm" or "confirmed" or "approve" => "Confirmed",
            "reject" or "rejected" or "deny" => "Rejected",
            _ => throw new ArgumentException("Decisão deve ser confirm ou reject.", nameof(decision))
        };

        const string sql = """
UPDATE pending_actions
SET status = @status, resolved_at = @now, resolved_by = @employee_id
WHERE condominium_id = @condominium_id AND id = @id AND status = 'Pending'
RETURNING id, conversation_id, action_type, risk_level, status, payload_json::text, created_at;
""";
        return QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@status", status);
            Add(cmd, "@now", DateTimeOffset.UtcNow);
            Add(cmd, "@employee_id", employeeId);
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@id", pendingActionId);
        }, r => new PendingActionResponse(
            r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), ReadTimestamp(r, 6)),
            cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResultResponse>> SearchAsync(
        Guid condominiumId,
        string query,
        CancellationToken cancellationToken)
    {
        var q = Required(query, 120);
        const string sql = """
SELECT kind, id, title, subtitle, ts
FROM (
    SELECT 'resident'::text AS kind, r.id, r.full_name AS title,
           COALESCE(u.display_name,'') AS subtitle, NULL::timestamptz AS ts
    FROM residents r
    JOIN resident_units ru ON ru.resident_id = r.id AND ru.ends_at IS NULL
    JOIN units u ON u.id = ru.unit_id
    WHERE u.condominium_id = @condominium_id
      AND r.full_name ILIKE '%' || @query || '%'
    UNION ALL
    SELECT 'visitor', va.id, va.visitor_name, COALESCE(va.purpose,''), va.starts_at
    FROM visitor_authorizations va
    WHERE va.condominium_id = @condominium_id
      AND (va.visitor_name ILIKE '%' || @query || '%' OR COALESCE(va.vehicle_plate,'') ILIKE '%' || @query || '%')
    UNION ALL
    SELECT 'package', p.id, p.carrier, COALESCE(p.description,''), p.received_at
    FROM packages p
    WHERE p.condominium_id = @condominium_id
      AND (p.carrier ILIKE '%' || @query || '%' OR COALESCE(p.description,'') ILIKE '%' || @query || '%')
    UNION ALL
    SELECT 'occurrence', o.id, o.title, o.category || ' • ' || o.status, o.created_at
    FROM occurrences o
    WHERE o.condominium_id = @condominium_id
      AND (o.title ILIKE '%' || @query || '%' OR o.description ILIKE '%' || @query || '%')
) x
ORDER BY ts DESC NULLS LAST, title
LIMIT 100;
""";
        return await QueryAsync(sql, cmd =>
        {
            Add(cmd, "@condominium_id", condominiumId);
            Add(cmd, "@query", q);
        }, r => new SearchResultResponse(
            r.GetString(0), r.GetGuid(1), r.GetString(2), r.GetString(3), NullableTimestamp(r, 4)),
            cancellationToken);
    }

    public async Task<OfflineSnapshotResponse> GetOfflineSnapshotAsync(
        Guid condominiumId,
        CancellationToken cancellationToken)
    {
        var residents = await QueryAsync("""
SELECT r.id, r.full_name, p.e164, u.id, u.display_name
FROM residents r
JOIN resident_phones p ON p.resident_id = r.id AND p.is_primary = TRUE
JOIN resident_units ru ON ru.resident_id = r.id AND ru.ends_at IS NULL
JOIN units u ON u.id = ru.unit_id
WHERE u.condominium_id = @condominium_id AND r.is_active = TRUE
ORDER BY r.full_name;
""", cmd => Add(cmd, "@condominium_id", condominiumId),
            r => new OfflineResidentResponse(r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetGuid(3), r.GetString(4)),
            cancellationToken);

        var authorizations = await ListVisitorAuthorizationsAsync(condominiumId, true, cancellationToken);
        var packages = await ListPackagesAsync(condominiumId, true, cancellationToken);
        var providers = await ListProvidersAsync(condominiumId, cancellationToken);

        return new OfflineSnapshotResponse(DateTimeOffset.UtcNow, residents, authorizations, packages, providers);
    }

    public async Task<DashboardResponse> GetDashboardAsync(
        Guid condominiumId,
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT
    (SELECT count(*) FROM visitor_authorizations WHERE condominium_id=@c AND status='Inside')::int,
    (SELECT count(*) FROM packages WHERE condominium_id=@c AND status='Waiting')::int,
    (SELECT count(*) FROM occurrences WHERE condominium_id=@c AND status NOT IN ('Resolved','Cancelled'))::int,
    (SELECT count(*) FROM visitor_authorizations WHERE condominium_id=@c AND status='Authorized' AND ends_at >= now())::int,
    (SELECT count(*) FROM pending_actions WHERE condominium_id=@c AND status='Pending')::int,
    (SELECT id FROM shifts WHERE condominium_id=@c AND (@e IS NULL OR employee_id=@e) AND closed_at IS NULL ORDER BY opened_at DESC LIMIT 1);
""";
        return (await QuerySingleAsync(sql, cmd =>
        {
            Add(cmd, "@c", condominiumId);
            Add(cmd, "@e", employeeId);
        }, r => new DashboardResponse(
            Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToInt32(r.GetValue(1), CultureInfo.InvariantCulture),
            Convert.ToInt32(r.GetValue(2), CultureInfo.InvariantCulture),
            Convert.ToInt32(r.GetValue(3), CultureInfo.InvariantCulture),
            Convert.ToInt32(r.GetValue(4), CultureInfo.InvariantCulture),
            NullableGuid(r, 5)),
            cancellationToken))!;
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        Action<DbCommand> bind,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            bind(command);
            var items = new List<T>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(map(reader));
            }
            return items;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task<T?> QuerySingleAsync<T>(
        string sql,
        Action<DbCommand> bind,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        var items = await QueryAsync(sql, bind, map, cancellationToken);
        return items.Count == 0 ? default : items[0];
    }

    private async Task ExecuteAsync(
        string sql,
        Action<DbCommand> bind,
        CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            bind(command);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static VisitorAuthorizationResponse MapVisitorAuthorization(DbDataReader r)
        => new(
            r.GetGuid(0),
            r.GetGuid(1),
            NullableGuid(r, 2),
            r.GetString(3),
            NullableString(r, 4),
            NullableString(r, 5),
            ReadTimestamp(r, 6),
            ReadTimestamp(r, 7),
            NullableString(r, 8),
            NullableString(r, 9),
            r.GetString(10),
            NullableTimestamp(r, 11),
            NullableTimestamp(r, 12));

    private static PackageResponse MapPackage(DbDataReader r)
        => new(
            r.GetGuid(0),
            r.GetGuid(1),
            r.GetString(2),
            NullableString(r, 3),
            ReadTimestamp(r, 4),
            r.GetString(5),
            NullableTimestamp(r, 6));

    private static OccurrenceResponse MapOccurrence(DbDataReader r)
        => new(
            r.GetGuid(0),
            r.GetString(1),
            r.GetString(2),
            r.GetString(3),
            r.GetString(4),
            r.GetString(5),
            NullableGuid(r, 6),
            NullableGuid(r, 7),
            ReadTimestamp(r, 8),
            ReadTimestamp(r, 9));

    private static ShiftResponse MapShift(DbDataReader r)
        => new(
            r.GetGuid(0),
            r.GetGuid(1),
            ReadTimestamp(r, 2),
            NullableTimestamp(r, 3),
            NullableString(r, 4),
            NullableGuid(r, 5),
            NullableTimestamp(r, 6));

    private static void Add(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string Required(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Valor obrigatório.");
        }
        var normalized = value.Trim();
        return normalized.Length <= max ? normalized : throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= max ? normalized : throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static string? NormalizePlate(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return null;
        var chars = plate.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray();
        var value = new string(chars);
        return value.Length <= 10 ? value : throw new ArgumentOutOfRangeException(nameof(plate));
    }

    private static string NormalizeChoice(string value, IReadOnlyList<string> allowed, string fallback)
        => allowed.FirstOrDefault(x => string.Equals(x, value?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? fallback;

    private static void ValidateWindow(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt) throw new ArgumentException("Fim deve ser posterior ao início.");
        if (endsAt - startsAt > TimeSpan.FromDays(31)) throw new ArgumentException("Janela máxima é 31 dias.");
    }

    private static string Hash(string input)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    private static string? NullableString(DbDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetString(index);

    private static Guid? NullableGuid(DbDataReader reader, int index)
        => reader.IsDBNull(index) ? null : reader.GetGuid(index);

    private static DateTimeOffset ReadTimestamp(DbDataReader reader, int index)
    {
        var value = reader.GetValue(index);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture)
        };
    }

    private static DateTimeOffset? NullableTimestamp(DbDataReader reader, int index)
        => reader.IsDBNull(index) ? null : ReadTimestamp(reader, index);
}

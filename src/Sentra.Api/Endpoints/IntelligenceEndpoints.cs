using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Intelligence;
using Sentra.Application.Operations;
using Sentra.Application.Security;
using Sentra.Contracts.Operations;
using Sentra.Domain.Conversations;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public static class IntelligenceEndpoints
{
    public static IEndpointRouteBuilder MapSentraIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/conversations/{conversationId:guid}/intelligence/analyze", AnalyzeAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsManage);

        endpoints.MapGet("/api/v1/pending-actions/{pendingActionId:guid}", GetPendingActionAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsRead);

        endpoints.MapPost("/api/v1/pending-actions/{pendingActionId:guid}/resolve", ResolvePendingActionAsync)
            .RequireAuthorization(PermissionCatalog.ConversationsManage);

        return endpoints;
    }

    private static async Task<IResult> AnalyzeAsync(
        Guid conversationId,
        ClaimsPrincipal user,
        SentraDbContext db,
        IIntelligenceEngine intelligence,
        IOperationalStore store,
        CancellationToken cancellationToken)
    {
        if (!TryIds(user, out var condominiumId, out _))
        {
            return Results.Unauthorized();
        }

        var conversation = await db.Conversations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == conversationId && x.CondominiumId == condominiumId, cancellationToken);

        if (conversation is null)
        {
            return Results.NotFound();
        }

        var messages = await db.Messages.AsNoTracking()
            .Where(x => x.ConversationId == conversationId && x.Text != null)
            .OrderByDescending(x => x.OccurredAt)
            .Take(12)
            .OrderBy(x => x.OccurredAt)
            .Select(x => x.Text!)
            .ToListAsync(cancellationToken);

        var decision = intelligence.Analyze(new IntelligenceContext(
            condominiumId,
            conversationId,
            conversation.ResidentId,
            conversation.UnitId,
            messages,
            DateTimeOffset.UtcNow));

        var extractedJson = JsonSerializer.Serialize(decision.Extracted);
        var missingJson = JsonSerializer.Serialize(decision.MissingFields);

        var interpretationId = await store.SaveInterpretationAsync(
            condominiumId,
            conversationId,
            decision.Intent,
            decision.ResolutionState,
            decision.Summary,
            extractedJson,
            missingJson,
            cancellationToken);

        Guid? pendingActionId = null;
        if (decision.ResolutionState == "READY_FOR_CONFIRMATION" &&
            decision.RiskLevel == "CONFIRMATION_REQUIRED" &&
            !string.IsNullOrWhiteSpace(decision.ProposedActionType))
        {
            pendingActionId = await store.CreatePendingActionAsync(
                condominiumId,
                conversationId,
                interpretationId,
                decision.ProposedActionType,
                decision.RiskLevel,
                extractedJson,
                cancellationToken);
        }

        return Results.Ok(new IntelligenceAnalysisResponse(
            interpretationId,
            pendingActionId,
            decision.Intent,
            decision.ResolutionState,
            decision.Summary,
            decision.Extracted,
            decision.MissingFields,
            decision.RiskLevel));
    }

    private static async Task<IResult> GetPendingActionAsync(
        Guid pendingActionId,
        ClaimsPrincipal user,
        IOperationalStore store,
        CancellationToken cancellationToken)
    {
        if (!TryIds(user, out var condominiumId, out _))
        {
            return Results.Unauthorized();
        }

        var pending = await store.GetPendingActionAsync(condominiumId, pendingActionId, cancellationToken);
        return pending is null ? Results.NotFound() : Results.Ok(pending);
    }

    private static async Task<IResult> ResolvePendingActionAsync(
        Guid pendingActionId,
        ResolvePendingActionRequest request,
        ClaimsPrincipal user,
        IOperationalStore store,
        CancellationToken cancellationToken)
    {
        if (!TryIds(user, out var condominiumId, out var employeeId))
        {
            return Results.Unauthorized();
        }

        var pending = await store.GetPendingActionAsync(condominiumId, pendingActionId, cancellationToken);
        if (pending is null)
        {
            return Results.NotFound();
        }

        if (!string.Equals(pending.Status, "Pending", StringComparison.Ordinal))
        {
            return Results.Conflict(new { error = "A ação já foi resolvida." });
        }

        var confirming = string.Equals(request.Decision, "confirm", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(request.Decision, "approve", StringComparison.OrdinalIgnoreCase);

        object? created = null;

        if (confirming)
        {
            if (string.Equals(pending.ActionType, "create_visitor_authorization", StringComparison.Ordinal))
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string?>>(pending.PayloadJson)
                    ?? new Dictionary<string, string?>();

                if (!TryGuid(data, "unit_id", out var unitId) ||
                    !TryText(data, "visitor_name", out var visitorName) ||
                    !TryDate(data, "arrival_time", out var arrival))
                {
                    return Results.Conflict(new { error = "A proposta não contém dados suficientes. Analise a conversa novamente." });
                }

                Guid? residentId = TryGuid(data, "resident_id", out var resident) ? resident : null;
                data.TryGetValue("vehicle_plate", out var plate);
                data.TryGetValue("relationship", out var relationship);
                data.TryGetValue("purpose", out var purpose);

                created = await store.CreateVisitorAuthorizationAsync(
                    condominiumId,
                    new CreateVisitorAuthorizationRequest(
                        unitId,
                        residentId,
                        visitorName,
                        null,
                        relationship,
                        arrival.AddMinutes(-30),
                        arrival.AddHours(6),
                        plate,
                        purpose),
                    cancellationToken);
            }
            else if (string.Equals(pending.ActionType, "create_occurrence", StringComparison.Ordinal))
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string?>>(pending.PayloadJson)
                    ?? new Dictionary<string, string?>();
                data.TryGetValue("description", out var description);
                Guid? unitId = TryGuid(data, "unit_id", out var parsedUnit) ? parsedUnit : null;

                created = await store.CreateOccurrenceAsync(
                    condominiumId,
                    new CreateOccurrenceRequest(
                        "Other",
                        "Ocorrência preparada pela inteligência",
                        string.IsNullOrWhiteSpace(description) ? "Sem descrição." : description!,
                        "Normal",
                        unitId),
                    cancellationToken);
            }
            else
            {
                return Results.Conflict(new { error = "Tipo de ação não está na allowlist do Policy Engine." });
            }
        }

        var resolved = await store.ResolvePendingActionAsync(
            condominiumId,
            pendingActionId,
            employeeId,
            confirming ? "confirm" : "reject",
            cancellationToken);

        return Results.Ok(resolved);
    }

    private static bool TryIds(ClaimsPrincipal user, out Guid condominiumId, out Guid employeeId)
    {
        condominiumId = Guid.Empty;
        employeeId = Guid.Empty;
        var condominium = user.FindFirst("condominium_id")?.Value;
        var employee = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(condominium, out condominiumId)
            && Guid.TryParse(employee, out employeeId);
    }

    private static bool TryGuid(Dictionary<string, string?> data, string key, out Guid value)
    {
        value = Guid.Empty;
        return data.TryGetValue(key, out var raw)
            && Guid.TryParse(raw, out value);
    }

    private static bool TryText(Dictionary<string, string?> data, string key, out string value)
    {
        value = string.Empty;
        if (!data.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return false;
        value = raw.Trim();
        return true;
    }

    private static bool TryDate(Dictionary<string, string?> data, string key, out DateTimeOffset value)
    {
        value = default;
        return data.TryGetValue(key, out var raw)
            && DateTimeOffset.TryParse(raw, out value);
    }
}

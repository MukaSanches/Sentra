namespace Sentra.Application.Intelligence;

public sealed record IntelligenceContext(
    Guid CondominiumId,
    Guid ConversationId,
    Guid? ResidentId,
    Guid? UnitId,
    IReadOnlyList<string> RecentMessages,
    DateTimeOffset Now);

public sealed record IntelligenceDecision(
    string Intent,
    string ResolutionState,
    string Summary,
    IReadOnlyDictionary<string, string?> Extracted,
    IReadOnlyList<string> MissingFields,
    string RiskLevel,
    string? ProposedActionType);

public interface IIntelligenceEngine
{
    IntelligenceDecision Analyze(IntelligenceContext context);
}

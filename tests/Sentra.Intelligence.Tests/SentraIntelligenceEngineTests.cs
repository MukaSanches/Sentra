using Sentra.Application.Intelligence;
using Sentra.Intelligence;

namespace Sentra.Intelligence.Tests;

public sealed class SentraIntelligenceEngineTests
{
    private readonly SentraIntelligenceEngine _engine = new();

    [Fact]
    public void VisitorConversationAcrossMessages_BecomesConfirmationAction()
    {
        var result = _engine.Analyze(new IntelligenceContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["Minha mãe vai chegar", "Maria", "ela vai de carro placa FGH2J45 umas 19:30"],
            new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.FromHours(-3))));

        Assert.Equal("visitor_authorization", result.Intent);
        Assert.Equal("READY_FOR_CONFIRMATION", result.ResolutionState);
        Assert.Equal("CONFIRMATION_REQUIRED", result.RiskLevel);
        Assert.Equal("Maria", result.Extracted["visitor_name"]);
        Assert.Equal("FGH2J45", result.Extracted["vehicle_plate"]);
    }

    [Fact]
    public void VisitorWithoutName_RequestsClarification()
    {
        var result = _engine.Analyze(new IntelligenceContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["Vai chegar uma visita umas 20h"],
            new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.FromHours(-3))));

        Assert.Equal("NEEDS_CLARIFICATION", result.ResolutionState);
        Assert.Contains("visitor_name", result.MissingFields);
    }

    [Fact]
    public void GateControl_IsNeverProposed()
    {
        var result = _engine.Analyze(new IntelligenceContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["Pode abrir o portão agora?"],
            DateTimeOffset.UtcNow));

        Assert.Equal("BLOCKED", result.ResolutionState);
        Assert.Equal("PROHIBITED", result.RiskLevel);
        Assert.Null(result.ProposedActionType);
    }
}

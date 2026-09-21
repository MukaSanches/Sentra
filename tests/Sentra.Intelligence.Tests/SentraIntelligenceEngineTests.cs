using Sentra.Application.Intelligence;
using Sentra.Intelligence;

namespace Sentra.Intelligence.Tests;

public sealed partial class SentraIntelligenceEngineTests
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


public partial class SentraIntelligenceEngineTests
{
    [Fact]
    public void RelationCue_DoesNotTreatVerbAsName()
    {
        var result = _engine.Analyze(new IntelligenceContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["Minha mãe vai chegar umas 19h"],
            new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.FromHours(-3))));

        Assert.Equal("NEEDS_CLARIFICATION", result.ResolutionState);
        Assert.Contains("visitor_name", result.MissingFields);
        Assert.Null(result.Extracted["visitor_name"]);
    }

    [Fact]
    public void StandaloneProperName_CompletesPreviousVisitContext()
    {
        var result = _engine.Analyze(new IntelligenceContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["Minha mãe vai chegar", "Maria de Souza", "ela chega 19:30"],
            new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.FromHours(-3))));

        Assert.Equal("READY_FOR_CONFIRMATION", result.ResolutionState);
        Assert.Equal("Maria De Souza", result.Extracted["visitor_name"]);
    }
}


public partial class SentraIntelligenceEngineTests
{
    [Fact]
    public void StandaloneNameParticle_IsAcceptedButSentenceIsNot()
    {
        var withName = _engine.Analyze(new IntelligenceContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["Vai chegar uma visita", "Ana da Silva", "chega 20:15"],
            new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.FromHours(-3))));

        Assert.Equal("Ana Da Silva", withName.Extracted["visitor_name"]);

        var withoutName = _engine.Analyze(new IntelligenceContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["Minha mãe vai chegar", "ela chega 20:15"],
            new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.FromHours(-3))));

        Assert.Null(withoutName.Extracted["visitor_name"]);
        Assert.Contains("visitor_name", withoutName.MissingFields);
    }
}

using Sentra.Contracts.Auth;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.Tests.Services;

public sealed class SessionStateTests
{
    [Fact]
    public void Set_KeepsValidTokenInMemory()
    {
        var condominiumId = Guid.NewGuid();
        var state = new SessionState();

        state.Set(new LoginResponse(
            "jwt-test-value",
            DateTimeOffset.UtcNow.AddMinutes(10),
            new EmployeeIdentityResponse(
                Guid.NewGuid(),
                "Porteiro",
                "porteiro",
                condominiumId,
                Guid.NewGuid()),
            ["conversations.read"]));

        Assert.True(state.IsAuthenticated);
        Assert.Equal("jwt-test-value", state.AccessToken);
        Assert.Equal(condominiumId, state.Employee?.CondominiumId);
    }

    [Fact]
    public void ExpiredSession_IsNotAuthenticated()
    {
        var state = new SessionState();

        state.Set(new LoginResponse(
            "expired",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            new EmployeeIdentityResponse(
                Guid.NewGuid(),
                "Porteiro",
                "porteiro",
                Guid.NewGuid(),
                Guid.NewGuid()),
            []));

        Assert.False(state.IsAuthenticated);
        Assert.Null(state.AccessToken);
    }
}

using Sentra.Infrastructure.Security;

namespace Sentra.Infrastructure.Tests.Security;

public sealed class PasswordHashServiceTests
{
    [Fact]
    public void HashAndVerify_RoundTripsPassword()
    {
        var service = new AspNetPasswordHashService();
        const string password = "Senha-Forte-123!";

        var hash = service.Hash(password);

        Assert.NotEqual(password, hash);
        Assert.True(service.Verify(password, hash));
        Assert.False(service.Verify("senha-errada", hash));
    }

    [Fact]
    public void Hash_RejectsShortPassword()
    {
        var service = new AspNetPasswordHashService();

        Assert.Throws<ArgumentException>(
            () => service.Hash("curta"));
    }
}

using Microsoft.AspNetCore.Identity;
using Sentra.Application.Security;

namespace Sentra.Infrastructure.Security;

public sealed class AspNetPasswordHashService : IPasswordHashService
{
    private static readonly object UserContext = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        ValidatePassword(password);
        return _hasher.HashPassword(UserContext, password);
    }

    public bool Verify(string password, string encodedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(encodedHash))
        {
            return false;
        }

        try
        {
            var result = _hasher.VerifyHashedPassword(UserContext, encodedHash, password);
            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            throw new ArgumentException(
                "A senha deve possuir pelo menos 12 caracteres.",
                nameof(password));
        }

        if (password.Length > 256)
        {
            throw new ArgumentOutOfRangeException(
                nameof(password),
                "A senha excede o tamanho permitido.");
        }
    }
}

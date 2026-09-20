using System.Security.Cryptography;
using System.Text;
using Sentra.Application.Security;

namespace Sentra.Infrastructure.Security;

public sealed class Pbkdf2PasswordHashService : IPasswordHashService
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string Algorithm = "PBKDF2-SHA512";

    public string Hash(string password)
    {
        ValidatePassword(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return string.Join(
            '$',
            Algorithm,
            Iterations.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string encodedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(encodedHash))
        {
            return false;
        }

        var parts = encodedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations < 100_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA512,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
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
            throw new ArgumentException("A senha deve possuir pelo menos 12 caracteres.", nameof(password));
        }

        if (password.Length > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(password), "A senha excede o tamanho permitido.");
        }
    }
}

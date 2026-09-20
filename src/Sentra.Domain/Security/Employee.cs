using Sentra.Domain.Common;

namespace Sentra.Domain.Security;

public sealed class Employee : EntityBase
{
    private Employee()
    {
    }

    public Employee(
        Guid condominiumId,
        Guid roleId,
        string fullName,
        string username,
        string passwordHash)
    {
        if (condominiumId == Guid.Empty)
        {
            throw new ArgumentException("Condomínio inválido.", nameof(condominiumId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Perfil inválido.", nameof(roleId));
        }

        CondominiumId = condominiumId;
        RoleId = roleId;
        FullName = DomainText.Required(fullName, nameof(fullName), 160);
        Username = DomainText.Required(username, nameof(username), 80);
        NormalizedUsername = NormalizeUsername(username);
        PasswordHash = DomainText.Required(passwordHash, nameof(passwordHash), 512);
    }

    public Guid CondominiumId { get; private set; }
    public Guid RoleId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    public bool IsLocked(DateTimeOffset now) => LockedUntil is not null && LockedUntil > now;

    public void RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = now.AddMinutes(15);
        }

        MarkUpdated(now);
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        MarkUpdated(now);
    }

    public static string NormalizeUsername(string username)
        => DomainText.Required(username, nameof(username), 80).ToUpperInvariant();
}

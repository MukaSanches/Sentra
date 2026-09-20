using Sentra.Contracts.Auth;

namespace Sentra.Desktop.Services;

public sealed class SessionState
{
    private readonly Lock _gate = new();
    private LoginResponse? _login;

    public bool IsAuthenticated
    {
        get
        {
            lock (_gate)
            {
                return _login is not null &&
                       _login.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(15);
            }
        }
    }

    public string? AccessToken
    {
        get
        {
            lock (_gate)
            {
                return IsAuthenticated ? _login!.AccessToken : null;
            }
        }
    }

    public EmployeeIdentityResponse? Employee
    {
        get
        {
            lock (_gate)
            {
                return IsAuthenticated ? _login!.Employee : null;
            }
        }
    }

    public IReadOnlyList<string> Permissions
    {
        get
        {
            lock (_gate)
            {
                return IsAuthenticated
                    ? _login!.Permissions
                    : Array.Empty<string>();
            }
        }
    }

    public void Set(LoginResponse login)
    {
        ArgumentNullException.ThrowIfNull(login);

        lock (_gate)
        {
            _login = login;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _login = null;
        }
    }
}

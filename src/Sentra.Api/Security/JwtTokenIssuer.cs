using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sentra.Contracts.Auth;
using Sentra.Domain.Security;

namespace Sentra.Api.Security;

public static class JwtTokenIssuer
{
    public static LoginResponse Issue(
        IConfiguration configuration,
        Employee employee,
        IReadOnlyList<string> permissions,
        DateTimeOffset now)
    {
        var issuer = configuration["Authentication:Issuer"]
            ?? configuration["AUTH_ISSUER"];
        var audience = configuration["Authentication:Audience"]
            ?? configuration["AUTH_AUDIENCE"];
        var signingKey = configuration["Authentication:SigningKey"]
            ?? configuration["AUTH_SIGNING_KEY"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(signingKey) ||
            signingKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Autenticação local não está configurada corretamente.");
        }

        var expiresAt = now.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new("condominium_id", employee.CondominiumId.ToString()),
            new("role_id", employee.RoleId.ToString()),
            new(ClaimTypes.Name, employee.FullName),
            new("username", employee.Username)
        };

        claims.AddRange(
            permissions.Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            new EmployeeIdentityResponse(
                employee.Id,
                employee.FullName,
                employee.Username,
                employee.CondominiumId,
                employee.RoleId),
            permissions);
    }
}

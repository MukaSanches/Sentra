namespace Sentra.Application.Security;

public interface IPermissionEvaluator
{
    Task<bool> HasPermissionAsync(
        string identitySubject,
        Guid condominiumId,
        string permissionCode,
        CancellationToken cancellationToken);
}

namespace Sentra.Contracts.Setup;

public sealed record SetupStatusResponse(bool Bootstrapped);

public sealed record BootstrapRequest(
    string CondominiumName,
    string AdministratorName,
    string Username,
    string Password);

public sealed record BootstrapResponse(
    Guid CondominiumId,
    Guid AdministratorId,
    string Message);

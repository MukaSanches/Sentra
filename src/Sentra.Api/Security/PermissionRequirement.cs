using Microsoft.AspNetCore.Authorization;

namespace Sentra.Api.Security;

public sealed record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;

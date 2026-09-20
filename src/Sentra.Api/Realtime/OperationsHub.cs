using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sentra.Application.Security;
using Sentra.Domain.Access;

namespace Sentra.Api.Realtime;

[Authorize]
public sealed class OperationsHub(IPermissionEvaluator permissionEvaluator) : Hub
{
    public async Task JoinCondominium(Guid condominiumId)
    {
        var subject = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(subject)
            || !await permissionEvaluator.HasPermissionAsync(
                subject,
                condominiumId,
                PermissionCodes.ConversationsRead,
                Context.ConnectionAborted))
        {
            throw new HubException("Sem permissão para acessar o canal operacional.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(condominiumId),
            Context.ConnectionAborted);
    }

    public static string GroupName(Guid condominiumId)
        => $"condominium:{condominiumId:D}";
}

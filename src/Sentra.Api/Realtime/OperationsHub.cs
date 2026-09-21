using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Sentra.Api.Realtime;

[Authorize]
public sealed class OperationsHub : Hub
{
    public static string GroupName(Guid condominiumId)
        => $"condominium:{condominiumId:N}";

    public override async Task OnConnectedAsync()
    {
        if (!Guid.TryParse(
                Context.User?.FindFirst("condominium_id")?.Value,
                out var condominiumId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(condominiumId));

        await base.OnConnectedAsync();
    }
}

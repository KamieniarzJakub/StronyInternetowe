using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class MatchHub : Hub
{
    public async Task JoinBracketGroup(int tournamentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
    }

    public async Task LeaveBracketGroup(int tournamentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
    }

    public async Task NotifyBracketUpdated(int tournamentId)
    {
        await Clients.Group($"tournament-{tournamentId}")
                     .SendAsync("bracketUpdated", tournamentId);
    }
}

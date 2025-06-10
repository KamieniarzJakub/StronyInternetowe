using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TournamentSystem.Hubs
{
    public class MatchHub : Hub
    {
        // Metoda do dołączania do grupy turnieju
        public async Task JoinBracketGroup(string tournamentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tournamentId);
            Console.WriteLine($"Connection {Context.ConnectionId} joined group {tournamentId}");
        }

        // Metoda do opuszczania grupy turnieju
        public async Task LeaveBracketGroup(string tournamentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tournamentId);
            Console.WriteLine($"Connection {Context.ConnectionId} left group {tournamentId}");
        }

        // Metoda do wysyłania aktualizacji drabinki (wywoływana z kontrolera)
        // Klienci nasłuchują na "bracketUpdated"
        public async Task BracketUpdated(string tournamentId)
        {
            await Clients.Group(tournamentId).SendAsync("bracketUpdated", tournamentId);
        }
    }
}

using TournamentSystem.Models;

namespace TournamentSystem.ViewModels
{
    public class PaginatedTournamentViewModel
    {
        public List<Tournament> Tournaments { get; set; } = new();
        public string? CurrentFilter { get; set; }

        public string? CurrentSort { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}

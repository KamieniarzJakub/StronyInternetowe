using TournamentSystem.Models;
using System.Collections.Generic;

namespace TournamentSystem.ViewModels
{
    public class MyTournamentsViewModel
    {
        // Tournaments the user is a participant in
        public List<Tournament> Upcoming { get; set; } = new List<Tournament>();
        public List<Tournament> Active { get; set; } = new List<Tournament>();
        public List<Tournament> Past { get; set; } = new List<Tournament>();

        // Tournaments the user has organized
        public List<Tournament> Organized { get; set; } = new List<Tournament>();

        // New: Upcoming matches for the user
        public List<Match> UpcomingMatches { get; set; } = new List<Match>();
    }
}

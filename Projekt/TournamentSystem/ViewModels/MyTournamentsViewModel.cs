using TournamentSystem.Models;
using System.Collections.Generic;

namespace TournamentSystem.ViewModels
{
    public class MyTournamentsViewModel
    {
        public List<Tournament> Upcoming { get; set; } = new List<Tournament>();
        public List<Tournament> Active { get; set; } = new List<Tournament>();
        public List<Tournament> Past { get; set; } = new List<Tournament>();
        public List<Tournament> Organized { get; set; } = new List<Tournament>();
        public List<Match> UpcomingMatches { get; set; } = new List<Match>();
    }
}

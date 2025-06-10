using TournamentSystem.Models;
using System.Collections.Generic;

namespace TournamentSystem.ViewModels
{
    public class MyTournamentsViewModel
    {
        public List<Tournament> Upcoming { get; set; }
        public List<Tournament> Active { get; set; }
        public List<Tournament> Past { get; set; }
        public List<Tournament> Organized { get; set; }
    }
}

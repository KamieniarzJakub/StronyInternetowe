using System.Collections.Generic;
using TournamentSystem.Models;
using System.Linq;

namespace TournamentSystem.ViewModels
{
    public class MatchesViewModel
    {
        public Tournament Tournament { get; set; }

        public List<IGrouping<int, Match>> MatchesByRound { get; set; }

        public string CurrentUserId { get; set; }
    }
}

using TournamentSystem.Models;
using System.Collections.Generic;

namespace TournamentSystem.ViewModels
{
    public class TournamentDetailsViewModel
    {
        public Tournament Tournament { get; set; }

        public List<Match>? Matches { get; set; }

        public List<Participant> Participants { get; set; }
        public int FreeSpots => Tournament.MaxParticipants - Participants.Count;
        public bool IsUserParticipant { get; set; }
        public bool IsOrganizer { get; set; }

    }
}



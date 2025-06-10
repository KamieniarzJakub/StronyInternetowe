using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public int Round { get; set; }

        public string? Player1Id { get; set; }
        public ApplicationUser? Player1 { get; set; }

        public string? Player2Id { get; set; }
        public ApplicationUser? Player2 { get; set; }

        public string? WinnerId { get; set; }
        public ApplicationUser? Winner { get; set; }

        public string? Player1ReportedWinnerId { get; set; }
        public string? Player2ReportedWinnerId { get; set; }

        public string? DiscrepancyMessage { get; set; }
    }

}

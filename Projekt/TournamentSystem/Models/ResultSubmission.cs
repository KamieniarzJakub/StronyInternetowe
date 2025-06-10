using System;
using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Models
{
    public class ResultSubmission
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public Match Match { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string WinnerId { get; set; }

        public DateTime SubmittedAt { get; set; }
    }
}

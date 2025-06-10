using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Models
{
    public class Participant
    {
        public int Id { get; set; }

        [Required]
        public int TournamentId { get; set; }
        public virtual Tournament Tournament { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        [StringLength(20)]
        public string LicenseNumber { get; set; }

        [Required]
        public int Ranking { get; set; }
    }
}

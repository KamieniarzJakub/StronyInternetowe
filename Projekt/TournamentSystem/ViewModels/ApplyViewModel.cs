using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.ViewModels
{
    public class ApplyViewModel
    {
        public int TournamentId { get; set; }

        [Required]
        [StringLength(20)]
        public string LicenseNumber { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Ranking must be greater than 0")]
        public int Ranking { get; set; }

        public string? TournamentName { get; set; }
    }
}

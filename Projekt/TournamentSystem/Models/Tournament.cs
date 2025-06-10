using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;



namespace TournamentSystem.Models
{

    public enum TournamentStatus
    {
        Upcoming,
        Active,
        Finished
    }

    public class ApplicationDeadlineValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var tournament = (Tournament)validationContext.ObjectInstance;
            DateTime? deadline = tournament.ApplicationDeadline;
            DateTime tournamentDate = tournament.Date;

            if (deadline.HasValue)
            {
                if (deadline.Value < DateTime.Now)
                    return new ValidationResult("Deadline zgłoszeń nie może być w przeszłości.");

                if (deadline.Value > tournamentDate)
                    return new ValidationResult("Deadline zgłoszeń nie może być późniejszy niż data turnieju.");
            }

            return ValidationResult.Success;
        }
    }
    public class Tournament
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Discipline { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [ApplicationDeadlineValidation]
        public DateTime ApplicationDeadline { get; set; }

        [Required]
        public string Location { get; set; }

        [Range(2, 1000)]
        public int MaxParticipants { get; set; }

        public string? SponsorLogos { get; set; }

        [BindNever]
        public string? OrganizerId { get; set; }

        public ICollection<Participant> Participants { get; set; } = new HashSet<Participant>();

        public string? Winner { get; set; }

        public TournamentStatus Status { get; set; } = TournamentStatus.Upcoming;

        public ICollection<Match> Matches { get; set; } = new HashSet<Match>();
    }
}

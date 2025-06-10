using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace TournamentSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public virtual ICollection<Participant> Participations { get; set; }
        public virtual ICollection<Tournament> OrganizedTournaments { get; set; }
    }
}

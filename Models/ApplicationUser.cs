using Microsoft.AspNetCore.Identity;

namespace soft20181_starter.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }
} 
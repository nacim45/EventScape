using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace soft20181_starter.Models
{
    public class AppUser : IdentityUser
    {
        // Map FirstName to Name for backward compatibility 
        [NotMapped]
        public string FirstName 
        { 
            get => Name; 
            set => Name = value; 
        }

        // Map LastName to Surname for backward compatibility
        [NotMapped]
        public string LastName 
        { 
            get => Surname; 
            set => Surname = value; 
        }
        
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // Default role
        public DateTime RegisteredDate { get; set; } = DateTime.Now;
        
        // This will be used for compatibility with the existing code
        // The actual password will be stored using Identity's mechanisms
        public string Password { get; set; } = string.Empty;
    }
} 
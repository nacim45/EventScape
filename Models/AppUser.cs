using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace soft20181_starter.Models
{
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens and apostrophes")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens and apostrophes")]
        public string Surname { get; set; } = string.Empty;

        // Backward compatibility properties
        [NotMapped]
        public string FirstName 
        { 
            get => Name; 
            set => Name = value; 
        }

        [NotMapped]
        public string LastName 
        { 
            get => Surname; 
            set => Surname = value; 
        }

        [Required]
        public string Role { get; set; } = "User";

        [Required]
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        public bool IsActive { get; set; } = true;

        public string? ProfilePictureUrl { get; set; }

        public string? Bio { get; set; }

        [NotMapped]
        public string FullName => $"{Name} {Surname}";

        // Navigation properties
        public virtual ICollection<TheEvent> CreatedEvents { get; set; } = new List<TheEvent>();
        public virtual ICollection<EventAttendance> EventAttendances { get; set; } = new List<EventAttendance>();
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

        // Preferences
        public bool ReceiveNotifications { get; set; } = true;
        public bool ReceiveMarketingEmails { get; set; } = false;
        public string? PreferredLanguage { get; set; } = "en";
        public string? TimeZone { get; set; }

        // Security
        public override bool TwoFactorEnabled { get; set; } = false;
        public DateTime? LastPasswordChangeDate { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEndDate { get; set; }

        // For compatibility with existing code
        [NotMapped]
        public string Password { get; set; } = string.Empty;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Helper methods
        public bool IsInRole(string roleName)
        {
            return Role.Equals(roleName, StringComparison.OrdinalIgnoreCase);
        }

        public bool CanManageEvent(TheEvent @event)
        {
            return IsInRole("Admin") || @event.CreatedById == Id;
        }
    }
} 
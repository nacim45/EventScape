using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace soft20181_starter.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityName { get; set; } = string.Empty; // "Event", "User", "Contact", "Feedback"

        [Required]
        [StringLength(50)]
        public string EntityId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete"

        [StringLength(450)]
        public string? UserId { get; set; } // Nullable for anonymous actions

        [StringLength(100)]
        public string? UserName { get; set; } // For display purposes

        [StringLength(50)]
        public string? UserRole { get; set; }

        [Column(TypeName = "text")]
        public string? OldValues { get; set; } // JSON serialized old values

        [Column(TypeName = "text")]
        public string? NewValues { get; set; } // JSON serialized new values

        [Column(TypeName = "text")]
        public string? Changes { get; set; } // Human readable changes

        [StringLength(500)]
        public string? AffectedColumns { get; set; } // Comma-separated list of changed columns

        [StringLength(100)]
        public string? TableName { get; set; }

        [StringLength(50)]
        public string? Schema { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(100)]
        public string? SessionId { get; set; }

        // Additional metadata
        [StringLength(100)]
        public string? ControllerName { get; set; }

        [StringLength(100)]
        public string? ActionName { get; set; }

        [StringLength(500)]
        public string? RequestUrl { get; set; }

        public bool IsSuccessful { get; set; } = true;

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        // Helper methods
        public string GetFormattedTimestamp()
        {
            return Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }

        public string GetActionDescription()
        {
            return $"{Action} {EntityName} (ID: {EntityId})";
        }

        public bool HasChanges()
        {
            return !string.IsNullOrEmpty(Changes) || !string.IsNullOrEmpty(NewValues);
        }
    }
}
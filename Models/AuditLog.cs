using System;
using System.ComponentModel.DataAnnotations;

namespace soft20181_starter.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string EntityName { get; set; } = string.Empty; // "Event", "User", etc.

        [Required]
        public string EntityId { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete", etc.

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Changes { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
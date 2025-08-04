using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace soft20181_starter.Models
{
    public class EventAttendance
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int EventId { get; set; }
        
        [ForeignKey("UserId")]
        public AppUser User { get; set; } = null!;
        
        [ForeignKey("EventId")]
        public TheEvent Event { get; set; } = null!;
        
        public DateTime RegisteredDate { get; set; } = DateTime.Now;
        
        // Ticket information
        [Required]
        public string TicketNumber { get; set; } = string.Empty;
        
        // Status of attendance: Registered, Attended, Cancelled
        [Required]
        public string Status { get; set; } = "Registered";

        // Payment status: unpaid, paid, refunded
        [Required]
        public string PaymentStatus { get; set; } = "unpaid";

        // Payment information - these will be added in a future migration
        public DateTime? PaymentDate { get; set; }
        public string PaymentIntentId { get; set; } = string.Empty;
    }
} 
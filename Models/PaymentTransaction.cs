using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace soft20181_starter.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PaymentIntentId { get; set; }

        public string? PayPalOrderId { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string UserEmail { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "GBP";

        [Required]
        public string Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string EventIds { get; set; }

        public string? RefundReason { get; set; }

        public string? RefundId { get; set; }

        public DateTime? RefundedAt { get; set; }

        [NotMapped]
        public string StatusDisplay => Status switch
        {
            "succeeded" => "Paid",
            "failed" => "Failed",
            "refunded" => "Refunded",
            "requires_payment_method" => "Awaiting Payment",
            "requires_confirmation" => "Awaiting Confirmation",
            "requires_action" => "Requires Action",
            "processing" => "Processing",
            _ => Status
        };
    }
} 
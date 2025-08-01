using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace soft20181_starter.Models;

public class TheEvent
{
    [Key]
    public int id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    public string title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [StringLength(100, ErrorMessage = "Location cannot be longer than 100 characters")]
    public string location { get; set; } = string.Empty;

    public List<string> images { get; set; } = new List<string>();

    [Required(ErrorMessage = "Description is required")]
    [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
    public string description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date is required")]
    public string date { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")]
    [RegularExpression(@"^(\d+(\.\d{1,2})?|Free)$", ErrorMessage = "Price must be a number or 'Free'")]
    public string price { get; set; } = string.Empty;

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string link { get; set; } = string.Empty;
    
    public bool IsDeleted { get; set; } = false;

    [Required]
    public string Category { get; set; } = "Other";

    public int? Capacity { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }

    public string? Tags { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedById { get; set; }

    [ForeignKey("CreatedById")]
    public AppUser? CreatedBy { get; set; }

    public int AvailableTickets => Capacity ?? 0;

    public bool IsFeatured { get; set; } = false;

    public string Status { get; set; } = "Active"; // Active, Cancelled, Sold Out, etc.

    // Navigation properties
    public virtual ICollection<EventAttendance> Attendances { get; set; } = new List<EventAttendance>();

    // Helper methods
    public void UpdateAuditFields(string userId)
    {
        if (id == 0) // New event
        {
            CreatedAt = DateTime.UtcNow;
            CreatedById = userId;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasAvailableTickets()
    {
        if (!Capacity.HasValue) return true; // No capacity limit
        return Attendances.Count < Capacity.Value;
    }

    public bool IsUpcoming()
    {
        if (DateTime.TryParse(date, out DateTime eventDate))
        {
            return eventDate > DateTime.UtcNow;
        }
        return false;
    }
}
using System.ComponentModel.DataAnnotations;

namespace soft20181_starter.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Feedback message is required")]
        [StringLength(2000, ErrorMessage = "Feedback cannot be longer than 2000 characters")]
        public string Message { get; set; } = string.Empty;

        public DateTime SubmissionDate { get; set; } = DateTime.Now;
        
        // User ID for authenticated users (nullable for anonymous submissions)
        public string? UserId { get; set; }
    }
} 
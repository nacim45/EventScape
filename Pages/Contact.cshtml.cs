using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using soft20181_starter.Services;
using System.Threading.Tasks;

namespace soft20181_starter.Pages
{
    public class ContactModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<ContactModel> _logger;
        private readonly SimpleAuditService _auditService;

        public ContactModel(EventAppDbContext context, ILogger<ContactModel> logger, SimpleAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        [BindProperty]
        public Contact ContactInfo { get; set; } = new Contact();

        [BindProperty]
        public string FeedbackMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // The page is displayed with an empty form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Extra validation for required fields with custom error messages
            if (string.IsNullOrWhiteSpace(ContactInfo.Name))
                ModelState.AddModelError("ContactInfo.Name", "Please enter your name");
            
            if (string.IsNullOrWhiteSpace(ContactInfo.Surname))
                ModelState.AddModelError("ContactInfo.Surname", "Please enter your surname");
            
            if (string.IsNullOrWhiteSpace(ContactInfo.Email))
                ModelState.AddModelError("ContactInfo.Email", "Please enter your email address");
            
            if (string.IsNullOrWhiteSpace(ContactInfo.Phone))
                ModelState.AddModelError("ContactInfo.Phone", "Please enter your phone number");

            // Validate email format
            if (!string.IsNullOrWhiteSpace(ContactInfo.Email) && 
                !System.Text.RegularExpressions.Regex.IsMatch(ContactInfo.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("ContactInfo.Email", "Please enter a valid email address");
            }

            // Validate phone format
            if (!string.IsNullOrWhiteSpace(ContactInfo.Phone) && 
                !System.Text.RegularExpressions.Regex.IsMatch(ContactInfo.Phone, @"^[0-9\+\-\s()]+$"))
            {
                ModelState.AddModelError("ContactInfo.Phone", "Please enter a valid phone number");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Contact form submission failed validation");
                return Page();
            }

            try
            {
                // Set submission date to current time
                ContactInfo.SubmissionDate = System.DateTime.Now;
                
                // Log data about to be saved
                _logger.LogInformation("Saving contact form data: Name: {Name}, Surname: {Surname}, Email: {Email}, Phone: {Phone}",
                    ContactInfo.Name, ContactInfo.Surname, ContactInfo.Email, ContactInfo.Phone);
                
                // Save to Database
                _context.Contacts.Add(ContactInfo);
                await _context.SaveChangesAsync();

                // Log successful submission with ID
                _logger.LogInformation("Contact form submitted successfully with ID {Id} for {Name} {Surname}",
                    ContactInfo.Id, ContactInfo.Name, ContactInfo.Surname);

                // Audit log the contact creation
                await _auditService.LogCreateAsync(ContactInfo);

                // Show success message
                TempData["SuccessMessage"] = "Thank you for contacting us!";
                TempData["Name"] = ContactInfo.Name;

                return RedirectToPage();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error saving contact form submission");
                ModelState.AddModelError(string.Empty, "An error occurred while processing your request. Please try again later.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostFeedbackAsync()
        {
            if (string.IsNullOrWhiteSpace(FeedbackMessage))
            {
                ModelState.AddModelError("FeedbackMessage", "Feedback message is required");
                return Page();
            }

            try
            {
                var feedback = new Feedback
                {
                    Message = FeedbackMessage,
                    SubmissionDate = System.DateTime.Now
                };

                _logger.LogInformation("Saving feedback: {Length} characters", FeedbackMessage.Length);
                
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Feedback submitted successfully with ID {Id}", feedback.Id);

                // Audit log the feedback creation
                await _auditService.LogCreateAsync(feedback);

                TempData["SuccessMessage"] = "Thank you for your feedback!";
                return RedirectToPage();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error saving feedback submission");
                ModelState.AddModelError(string.Empty, "An error occurred while processing your feedback. Please try again later.");
                return Page();
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using soft20181_starter.Models;
using soft20181_starter.Services;
using System.Security.Claims;

namespace soft20181_starter.Pages
{
    public class TestAuditModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly SimpleAuditService _auditService;

        public TestAuditModel(EventAppDbContext context, SimpleAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [TempData]
        public string TestMessage { get; set; } = string.Empty;

        public IEnumerable<AuditLog> RecentAuditLogs { get; set; } = new List<AuditLog>();

        public async Task OnGetAsync()
        {
            // Load recent audit logs for display
            RecentAuditLogs = await _auditService.GetAuditLogsAsync(pageSize: 10);
        }

        public async Task<IActionResult> OnPostCreateContactAsync(string name, string surname, string email, string phone, string message)
        {
            try
            {
                var contact = new Contact
                {
                    Name = name,
                    Surname = surname,
                    Email = email,
                    Phone = phone,
                    Message = message,
                    SubmissionDate = DateTime.Now
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                // Audit log the contact creation
                await _auditService.LogCreateAsync(contact);

                TestMessage = $"Test contact created successfully with ID: {contact.Id}. Audit log should be generated.";
                
                // Reload recent audit logs
                RecentAuditLogs = await _auditService.GetAuditLogsAsync(pageSize: 10);
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TestMessage = $"Error creating test contact: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCreateFeedbackAsync(string message)
        {
            try
            {
                var feedback = new Feedback
                {
                    Message = message,
                    SubmissionDate = DateTime.Now
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                // Audit log the feedback creation
                await _auditService.LogCreateAsync(feedback);

                TestMessage = $"Test feedback created successfully with ID: {feedback.Id}. Audit log should be generated.";
                
                // Reload recent audit logs
                RecentAuditLogs = await _auditService.GetAuditLogsAsync(pageSize: 10);
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TestMessage = $"Error creating test feedback: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using soft20181_starter.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages
{
    public class TestAuditModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly SimpleAuditService _auditService;
        private readonly ILogger<TestAuditModel> _logger;

        public TestAuditModel(EventAppDbContext context, SimpleAuditService auditService, ILogger<TestAuditModel> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        [BindProperty]
        public string StatusMessage { get; set; } = string.Empty;

        public List<AuditLog> RecentLogs { get; set; } = new List<AuditLog>();

        public async Task OnGetAsync()
        {
            try
            {
                // Load recent audit logs
                RecentLogs = await _context.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} recent audit logs", RecentLogs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading audit logs");
                StatusMessage = $"Error loading audit logs: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostCreateContactAsync(string name, string surname, string email, string phone, string message)
        {
            try
            {
                _logger.LogInformation("Creating test contact: {Name} {Surname}", name, surname);

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

                _logger.LogInformation("Contact created with ID: {Id}", contact.Id);

                // Create audit log
                try
                {
                    await _auditService.LogCreateAsync(contact);
                    _logger.LogInformation("Audit log created successfully for contact {Id}", contact.Id);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for contact {Id}", contact.Id);
                }

                StatusMessage = $"Test contact created successfully with ID: {contact.Id}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test contact");
                StatusMessage = $"Error creating test contact: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCreateFeedbackAsync(string message)
        {
            try
            {
                _logger.LogInformation("Creating test feedback: {Length} characters", message.Length);

                var feedback = new Feedback
                {
                    Message = message,
                    SubmissionDate = DateTime.Now
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Feedback created with ID: {Id}", feedback.Id);

                // Create audit log
                try
                {
                    await _auditService.LogCreateAsync(feedback);
                    _logger.LogInformation("Audit log created successfully for feedback {Id}", feedback.Id);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to create audit log for feedback {Id}", feedback.Id);
                }

                StatusMessage = $"Test feedback created successfully with ID: {feedback.Id}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test feedback");
                StatusMessage = $"Error creating test feedback: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}

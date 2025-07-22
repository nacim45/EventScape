using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Events
{
    [Authorize(Roles = "Administrator")]
    public class DeleteModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(EventAppDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TheEvent Event { get; set; }
        
        public int AttendeeCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                _logger.LogInformation("Loading event for deletion. ID: {EventId}", id);

                Event = await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.id == id);

                if (Event == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found for deletion", id);
                    return Page();
                }

                // Count attendees
                AttendeeCount = await _context.EventAttendances
                    .CountAsync(ea => ea.EventId == id);

                _logger.LogInformation("Found event {EventTitle} with {AttendeeCount} attendees", 
                    Event.title, AttendeeCount);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event for deletion. ID: {EventId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while loading the event. Please try again.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var eventId = Event.id;
                _logger.LogInformation("Deleting event with ID: {EventId}", eventId);

                // Find the event to delete
                var eventToDelete = await _context.Events
                    .FirstOrDefaultAsync(e => e.id == eventId);

                if (eventToDelete == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found when attempting to delete", eventId);
                    return NotFound();
                }

                // Find and delete all attendances for this event
                var attendances = await _context.EventAttendances
                    .Where(ea => ea.EventId == eventId)
                    .ToListAsync();
                
                if (attendances.Any())
                {
                    _logger.LogInformation("Removing {Count} attendances for event {EventId}", 
                        attendances.Count, eventId);
                    _context.EventAttendances.RemoveRange(attendances);
                }

                // Delete the event
                _context.Events.Remove(eventToDelete);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Event {EventTitle} (ID: {EventId}) deleted successfully", 
                    eventToDelete.title, eventId);
                
                TempData["SuccessMessage"] = "Event deleted successfully!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", Event?.id ?? 0);
                ModelState.AddModelError(string.Empty, "An error occurred while deleting the event. Please try again.");
                return Page();
            }
        }
    }
} 
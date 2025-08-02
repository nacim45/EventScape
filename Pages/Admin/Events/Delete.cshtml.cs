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
        private readonly IWebHostEnvironment _environment;

        public DeleteModel(
            EventAppDbContext context, 
            ILogger<DeleteModel> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [BindProperty]
        public TheEvent Event { get; set; } = new TheEvent();
        
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
                _logger.LogInformation("Attempting to delete event with ID: {EventId}", eventId);

                // Begin transaction
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Find the event to delete with its attendances
                var eventToDelete = await _context.Events
                    .Include(e => e.Attendances)
                    .FirstOrDefaultAsync(e => e.id == eventId && !e.IsDeleted);

                // Validate event exists and is not already deleted
                if (eventToDelete == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found or already deleted", eventId);
                    ModelState.AddModelError(string.Empty, "Event not found or has already been deleted.");
                    return Page();
                }

                // Check if event is in the past
                if (DateTime.TryParse(eventToDelete.date, out DateTime eventDate) && eventDate < DateTime.Today)
                {
                    _logger.LogWarning("Attempting to delete past event {EventId}", eventId);
                    ModelState.AddModelError(string.Empty, "Cannot delete past events for record-keeping purposes.");
                    return Page();
                }

                if (eventToDelete == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found when attempting to delete", eventId);
                    ModelState.AddModelError(string.Empty, "Event not found or has already been deleted.");
                    return RedirectToPage("/Admin");
                }

                    // Check if event has attendees
                    var hasAttendees = eventToDelete.Attendances.Any(a => a.Status != "Cancelled");

                    if (hasAttendees)
                    {
                        // Soft delete if event has attendees
                        _logger.LogInformation("Event {EventId} has attendees. Performing soft delete.", eventId);
                        
                        eventToDelete.IsDeleted = true;
                        eventToDelete.Status = "Cancelled";
                        eventToDelete.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Events.Update(eventToDelete);
                        
                        // Create audit log for soft delete
                        var softDeleteAudit = new AuditLog
                        {
                            EntityName = "Event",
                            EntityId = eventId.ToString(),
                            Action = "SoftDelete",
                            UserId = User.Identity?.Name,
                            Changes = $"Soft deleted event: {eventToDelete.title} (ID: {eventId}) due to existing attendees",
                            Timestamp = DateTime.UtcNow
                        };
                        await _context.AuditLogs.AddAsync(softDeleteAudit);
                    }
                    else
                    {
                        // Hard delete if no attendees
                        _logger.LogInformation("Event {EventId} has no attendees. Performing hard delete.", eventId);
                        
                                                    // Delete associated images
                            var images = eventToDelete.images ?? new List<string>();
                            if (images.Any())
                            {
                                foreach (var imagePath in images)
                                {
                                    // Skip data URLs and external URLs
                                    if (imagePath.StartsWith("data:") || imagePath.StartsWith("http"))
                                    {
                                        continue;
                                    }

                                    if (imagePath.StartsWith("images/events/"))
                                    {
                                        try
                                        {
                                            var fullPath = Path.Combine(_environment.WebRootPath, imagePath);
                                            if (System.IO.File.Exists(fullPath))
                                            {
                                                System.IO.File.Delete(fullPath);
                                                _logger.LogInformation("Deleted image file: {ImagePath}", imagePath);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // Log but don't fail if file deletion fails
                                            _logger.LogWarning("Could not delete image file {ImagePath}. Error: {Error}", 
                                                imagePath, ex.Message);
                                        }
                                    }
                                }
                            }

                        // Remove the event
                        _context.Events.Remove(eventToDelete);
                        
                        // Create audit log for hard delete
                        var hardDeleteAudit = new AuditLog
                        {
                            EntityName = "Event",
                            EntityId = eventId.ToString(),
                            Action = "HardDelete",
                            UserId = User.Identity?.Name,
                            Changes = $"Hard deleted event: {eventToDelete.title} (ID: {eventId})",
                            Timestamp = DateTime.UtcNow
                        };
                        await _context.AuditLogs.AddAsync(hardDeleteAudit);
                    }

                    // Save all changes
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                    
                    var deleteType = hasAttendees ? "soft" : "hard";
                    _logger.LogInformation("Event {EventTitle} (ID: {EventId}) {DeleteType} deleted successfully", 
                        eventToDelete.title, eventId, deleteType);
                    
                    TempData["SuccessMessage"] = hasAttendees 
                        ? $"Event '{eventToDelete.title}' was marked as deleted (soft delete due to existing attendees)."
                        : $"Event '{eventToDelete.title}' was permanently deleted.";
                    
                    return RedirectToPage("/Admin");
                }
                                    catch
                    {
                        // Rollback transaction on error
                        await transaction.RollbackAsync();
                        throw;
                    }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}. Error: {Error}", 
                    Event?.id ?? 0, ex.Message);
                ModelState.AddModelError(string.Empty, $"An error occurred while deleting the event: {ex.Message}");
                return Page();
            }
        }
    }
} 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Events
{
    [Authorize(Roles = "Administrator,Admin")]
    public class EditSearchResultsModel : PageModel
    {
        private readonly ILogger<EditSearchResultsModel> _logger;
        private readonly EventAppDbContext _context;

        public List<TheEvent> Events { get; set; } = new List<TheEvent>();
        public bool HasSearchResults => Events != null && Events.Any();

        public EditSearchResultsModel(ILogger<EditSearchResultsModel> logger, EventAppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(string eventName, string location)
        {
            if (string.IsNullOrWhiteSpace(eventName) && string.IsNullOrWhiteSpace(location))
            {
                _logger.LogWarning("Search for events to edit with no criteria provided");
                return RedirectToPage("SearchForEdit");
            }

            _logger.LogInformation("Searching for events to edit with Name: {EventName}, Location: {Location}", 
                eventName ?? "(not specified)", location ?? "(not specified)");

            // Query the database directly using DbContext
            var query = _context.Events.AsQueryable();

            // Apply filters if provided - using case-insensitive comparisons
            if (!string.IsNullOrWhiteSpace(eventName))
            {
                query = query.Where(e => EF.Functions.Like(e.title, $"%{eventName}%"));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(e => EF.Functions.Like(e.location, $"%{location}%"));
            }

            // Execute query and get results
            Events = await query.ToListAsync();

            _logger.LogInformation("Found {Count} events matching the search criteria", Events.Count);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state when updating event");
                    return BadRequest("Invalid form data");
                }

                // Get form data
                var eventId = int.Parse(Request.Form["EventId"]);
                var eventTitle = Request.Form["EventTitle"].ToString();
                var eventDescription = Request.Form["EventDescription"].ToString();
                var eventLocation = Request.Form["EventLocation"].ToString();
                var eventDate = Request.Form["EventDate"].ToString();
                var eventPrice = Request.Form["EventPrice"].ToString();
                var eventCategory = Request.Form["EventCategory"].ToString();
                var eventCapacity = Request.Form["EventCapacity"].ToString();
                var eventStartTime = Request.Form["EventStartTime"].ToString();
                var eventEndTime = Request.Form["EventEndTime"].ToString();
                var eventTags = Request.Form["EventTags"].ToString();

                _logger.LogInformation("Updating event {EventId} with title: {EventTitle}", eventId, eventTitle);

                // Begin transaction
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Get the existing event
                    var existingEvent = await _context.Events
                        .Include(e => e.Attendances)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.id == eventId);

                    if (existingEvent == null)
                    {
                        _logger.LogWarning("Event with ID {EventId} not found", eventId);
                        return NotFound("Event not found");
                    }

                    if (existingEvent.IsDeleted)
                    {
                        _logger.LogWarning("Attempted to edit deleted event: {EventId}", eventId);
                        return BadRequest("Cannot edit a deleted event");
                    }

                    // Store original values for audit log
                    var changes = new List<string>();

                    // Validate and format the date
                    if (!DateTime.TryParse(eventDate, out DateTime parsedDate))
                    {
                        return BadRequest("Invalid date format");
                    }

                    if (parsedDate.Date < DateTime.Today)
                    {
                        return BadRequest("Event date cannot be in the past");
                    }

                    // Format date consistently
                    var newDate = parsedDate.ToString("dddd, dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

                    // Update event properties
                    if (newDate != existingEvent.date)
                    {
                        changes.Add($"Date changed from '{existingEvent.date}' to '{newDate}'");
                    }

                    var newLocation = eventLocation.Trim().ToLower();
                    if (newLocation != existingEvent.location)
                    {
                        changes.Add($"Location changed from '{existingEvent.location}' to '{newLocation}'");
                    }

                    if (eventDescription != existingEvent.description)
                    {
                        changes.Add("Description updated");
                    }

                    if (eventPrice != existingEvent.price)
                    {
                        changes.Add($"Price changed from '{existingEvent.price}' to '{eventPrice}'");
                    }

                    if (eventCategory != existingEvent.Category)
                    {
                        changes.Add($"Category changed from '{existingEvent.Category}' to '{eventCategory}'");
                    }

                    int? newCapacity = null;
                    if (int.TryParse(eventCapacity, out int capacity))
                    {
                        newCapacity = capacity;
                    }

                    if (newCapacity != existingEvent.Capacity)
                    {
                        changes.Add($"Capacity changed from '{existingEvent.Capacity}' to '{newCapacity}'");
                    }

                    if (eventStartTime != existingEvent.StartTime)
                    {
                        changes.Add($"Start time changed from '{existingEvent.StartTime}' to '{eventStartTime}'");
                    }

                    if (eventEndTime != existingEvent.EndTime)
                    {
                        changes.Add($"End time changed from '{existingEvent.EndTime}' to '{eventEndTime}'");
                    }

                    if (eventTags != existingEvent.Tags)
                    {
                        changes.Add($"Tags changed from '{existingEvent.Tags}' to '{eventTags}'");
                    }

                    // Create updated event
                    var updatedEvent = new TheEvent
                    {
                        id = existingEvent.id,
                        title = eventTitle,
                        description = eventDescription,
                        location = newLocation,
                        date = newDate,
                        price = eventPrice,
                        link = existingEvent.link,
                        images = existingEvent.images,
                        Category = eventCategory,
                        Capacity = newCapacity,
                        StartTime = eventStartTime,
                        EndTime = eventEndTime,
                        Tags = eventTags,
                        IsDeleted = existingEvent.IsDeleted,
                        CreatedAt = existingEvent.CreatedAt,
                        UpdatedAt = DateTime.UtcNow,
                        Attendances = existingEvent.Attendances
                    };

                    // Update the event
                    _context.Attach(updatedEvent).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    // Create audit log if there were changes
                    if (changes.Any())
                    {
                        var auditLog = new AuditLog
                        {
                            EntityName = "Event",
                            EntityId = updatedEvent.id.ToString(),
                            Action = "Update",
                            UserId = User.Identity?.Name ?? "System",
                            Changes = string.Join("; ", changes),
                            Timestamp = DateTime.UtcNow
                        };
                        await _context.AuditLogs.AddAsync(auditLog);
                        await _context.SaveChangesAsync();
                    }

                    // Commit transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Event updated successfully: {EventId} - {EventTitle}. Changes: {Changes}", 
                        updatedEvent.id, updatedEvent.title, string.Join("; ", changes));

                    return Content("success", "text/plain");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating event {EventId}. Error: {Error}", eventId, ex.Message);
                    return StatusCode(500, "An error occurred while updating the event");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostAsync for event update. Error: {Error}", ex.Message);
                return StatusCode(500, "An unexpected error occurred");
            }
        }
    }
} 
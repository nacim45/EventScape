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
    [Authorize(Roles = "Administrator")]
    public class EditModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(EventAppDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TheEvent Event { get; set; } = new TheEvent();

        [BindProperty]
        public string ImageUrls { get; set; } = string.Empty;

        public string ImageUrlsString { get; set; } = string.Empty;

        [BindProperty]
        public string EventCategory { get; set; } = "Other";  // Default category

        [BindProperty]
        public int? EventCapacity { get; set; }

        [BindProperty]
        public string? EventStartTime { get; set; }

        [BindProperty]
        public string? EventEndTime { get; set; }

        [BindProperty]
        public string? EventTags { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                _logger.LogInformation("Loading event for editing. ID: {EventId}", id);

                Event = await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.id == id);

                if (Event == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found for editing", id);
                    return Page();
                }

                // Convert image list to string for textarea
                if (Event.images != null && Event.images.Any())
                {
                    ImageUrlsString = string.Join(Environment.NewLine, Event.images);
                }

                // Retrieve any saved event properties from TempData
                if (TempData.ContainsKey("EventCategory_" + id))
                {
                    EventCategory = TempData["EventCategory_" + id]?.ToString();
                }

                if (TempData.ContainsKey("EventCapacity_" + id))
                {
                    if (int.TryParse(TempData["EventCapacity_" + id]?.ToString(), out int capacity))
                    {
                        EventCapacity = capacity;
                    }
                }

                if (TempData.ContainsKey("EventStartTime_" + id))
                {
                    EventStartTime = TempData["EventStartTime_" + id]?.ToString();
                }

                if (TempData.ContainsKey("EventEndTime_" + id))
                {
                    EventEndTime = TempData["EventEndTime_" + id]?.ToString();
                }

                if (TempData.ContainsKey("EventTags_" + id))
                {
                    EventTags = TempData["EventTags_" + id]?.ToString();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event for editing. ID: {EventId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while loading the event. Please try again.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["ErrorMessage"] = $"Please fix the following errors: {string.Join(", ", errors)}";
                    _logger.LogWarning("Invalid model state when updating event: {Errors}", string.Join(", ", errors));
                    return Page();
                }

                // Begin transaction
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Get the existing event to track changes
                    var existingEvent = await _context.Events
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.id == Event.id);

                    if (existingEvent == null)
                    {
                        _logger.LogWarning("Event with ID {EventId} no longer exists", Event.id);
                        return NotFound("Event not found or has been deleted.");
                    }

                    // Store original values for audit log
                    var changes = new List<string>();

                    // Validate and format the date
                    if (!DateTime.TryParse(Event.date, out DateTime eventDate))
                    {
                        ModelState.AddModelError("Event.date", "Invalid date format");
                        return Page();
                    }

                    if (eventDate.Date < DateTime.Today)
                    {
                        ModelState.AddModelError("Event.date", "Event date cannot be in the past");
                        return Page();
                    }

                    // Format date in consistent format
                    var newDate = eventDate.ToString("dddd, dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    if (newDate != existingEvent.date)
                    {
                        changes.Add($"Date changed from '{existingEvent.date}' to '{newDate}'");
                        Event.date = newDate;
                    }

                    // Process and validate location
                    if (string.IsNullOrEmpty(Event.location))
                    {
                        ModelState.AddModelError("Event.location", "Location is required");
                        return Page();
                    }
                    var newLocation = Event.location.Trim().ToLower();
                    if (newLocation != existingEvent.location)
                    {
                        changes.Add($"Location changed from '{existingEvent.location}' to '{newLocation}'");
                        Event.location = newLocation;
                    }

                    // Validate price
                    if (!string.IsNullOrEmpty(Event.price))
                    {
                        var newPrice = Event.price;
                        if (newPrice.ToLower() != "free")
                        {
                            // Remove currency symbol if present and try to parse
                            var priceStr = newPrice.Replace("£", "").Replace("$", "").Trim();
                            if (!decimal.TryParse(priceStr, out decimal price))
                            {
                                ModelState.AddModelError("Event.price", "Price must be a number or 'Free'");
                                return Page();
                            }
                            newPrice = $"£{price:N2}";
                        }
                        else
                        {
                            newPrice = "Free";
                        }

                        if (newPrice != existingEvent.price)
                        {
                            changes.Add($"Price changed from '{existingEvent.price}' to '{newPrice}'");
                            Event.price = newPrice;
                        }
                    }

                    // Update additional properties
                    if (EventCategory != existingEvent.Category)
                    {
                        changes.Add($"Category changed from '{existingEvent.Category}' to '{EventCategory}'");
                        Event.Category = EventCategory;
                    }

                    if (EventCapacity != existingEvent.Capacity)
                    {
                        // Check if reducing capacity is possible
                        if (EventCapacity.HasValue && EventCapacity.Value < existingEvent.Capacity)
                        {
                            var attendeeCount = await _context.EventAttendances
                                .CountAsync(ea => ea.EventId == Event.id && ea.Status != "Cancelled");
                            
                            if (EventCapacity.Value < attendeeCount)
                            {
                                ModelState.AddModelError("EventCapacity", $"Cannot reduce capacity below current attendee count ({attendeeCount})");
                                return Page();
                            }
                        }
                        changes.Add($"Capacity changed from '{existingEvent.Capacity}' to '{EventCapacity}'");
                        Event.Capacity = EventCapacity;
                    }

                    if (EventStartTime != existingEvent.StartTime)
                    {
                        changes.Add($"Start time changed from '{existingEvent.StartTime}' to '{EventStartTime}'");
                        Event.StartTime = EventStartTime;
                    }

                    if (EventEndTime != existingEvent.EndTime)
                    {
                        changes.Add($"End time changed from '{existingEvent.EndTime}' to '{EventEndTime}'");
                        Event.EndTime = EventEndTime;
                    }

                    if (EventTags != existingEvent.Tags)
                    {
                        changes.Add($"Tags changed from '{existingEvent.Tags}' to '{EventTags}'");
                        Event.Tags = EventTags;
                    }

                    // Process image URLs
                    var newImages = new List<string>();
                    if (!string.IsNullOrWhiteSpace(ImageUrls))
                    {
                        var urls = ImageUrls
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(url => url.Trim())
                            .Where(url => !string.IsNullOrWhiteSpace(url))
                            .ToList();
                        
                        foreach (var url in urls)
                        {
                            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                            {
                                ModelState.AddModelError("ImageUrls", $"Invalid URL format: {url}");
                                return Page();
                            }
                        }
                        
                        newImages.AddRange(urls);
                    }

                    if (!newImages.SequenceEqual(existingEvent.images))
                    {
                        changes.Add("Images updated");
                        Event.images = newImages;
                    }

                    // Update audit fields
                    Event.UpdatedAt = DateTime.UtcNow;

                    // Update the event
                    _context.Attach(Event).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    // Create audit log if there were changes
                    if (changes.Any())
                    {
                        var auditLog = new AuditLog
                        {
                            EntityName = "Event",
                            EntityId = Event.id.ToString(),
                            Action = "Update",
                            UserId = User.Identity?.Name,
                            Changes = string.Join("; ", changes),
                            Timestamp = DateTime.UtcNow
                        };
                        await _context.AuditLogs.AddAsync(auditLog);
                        await _context.SaveChangesAsync();
                    }

                    // Commit transaction
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Event updated successfully: {EventId} - {EventTitle}. Changes: {Changes}", 
                        Event.id, Event.title, string.Join("; ", changes));

                    TempData["SuccessMessage"] = $"Event '{Event.title}' was updated successfully!";
                    
                    // Redirect to Admin page
                    return RedirectToPage("/Admin");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency conflict when updating event {EventId}", Event.id);
                    ModelState.AddModelError(string.Empty, "The event was modified by another user. Please refresh and try again.");
                    return Page();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event: {Title}. Error: {Error}", 
                    Event.title, ex.Message);
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the event: {ex.Message}");
                return Page();
            }
        }

        private async Task<bool> EventExists(int id)
        {
            return await _context.Events.AnyAsync(e => e.id == id);
        }
    }
} 
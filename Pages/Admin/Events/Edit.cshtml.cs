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
        public TheEvent Event { get; set; }

        [BindProperty]
        public string ImageUrls { get; set; } = string.Empty;

        public string ImageUrlsString { get; set; } = string.Empty;

        [BindProperty]
        public string EventCategory { get; set; }

        [BindProperty]
        public int? EventCapacity { get; set; }

        [BindProperty]
        public string EventStartTime { get; set; }

        [BindProperty]
        public string EventEndTime { get; set; }

        [BindProperty]
        public string EventTags { get; set; }

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
                    _logger.LogWarning("Invalid model state when updating event");
                    return Page();
                }

                // Process image URLs
                if (!string.IsNullOrWhiteSpace(ImageUrls))
                {
                    Event.images = ImageUrls
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(url => url.Trim())
                        .Where(url => !string.IsNullOrWhiteSpace(url))
                        .ToList();
                }
                else
                {
                    Event.images = new List<string>();
                }
                
                // Save the additional event properties to TempData
                // Since we're not modifying the TheEvent model, we'll store these as TempData with event ID
                if (!string.IsNullOrEmpty(EventCategory))
                {
                    TempData["EventCategory_" + Event.id] = EventCategory;
                }

                if (EventCapacity.HasValue)
                {
                    TempData["EventCapacity_" + Event.id] = EventCapacity.Value;
                }

                if (!string.IsNullOrEmpty(EventStartTime))
                {
                    TempData["EventStartTime_" + Event.id] = EventStartTime;
                }

                if (!string.IsNullOrEmpty(EventEndTime))
                {
                    TempData["EventEndTime_" + Event.id] = EventEndTime;
                }

                if (!string.IsNullOrEmpty(EventTags))
                {
                    TempData["EventTags_" + Event.id] = EventTags;
                }

                // Update the event
                _context.Attach(Event).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Event updated successfully: {EventTitle}", Event.title);
                    TempData["SuccessMessage"] = "Event updated successfully!";
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!await EventExists(Event.id))
                    {
                        _logger.LogWarning("Event with ID {EventId} no longer exists", Event.id);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency conflict when updating event {EventId}", Event.id);
                        ModelState.AddModelError(string.Empty, "The event was modified by another user. Please try again.");
                        return Page();
                    }
                }

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", Event?.id ?? 0);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the event. Please try again.");
                return Page();
            }
        }

        private async Task<bool> EventExists(int id)
        {
            return await _context.Events.AnyAsync(e => e.id == id);
        }
    }
} 
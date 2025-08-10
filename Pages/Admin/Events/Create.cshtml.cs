using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Events
{
    [Authorize(Roles = "Administrator")]
    public class CreateModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<CreateModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public CreateModel(
            EventAppDbContext context, 
            ILogger<CreateModel> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [BindProperty]
        public TheEvent Event { get; set; } = new TheEvent();

        // Removed ImageUrls to keep drag-and-drop only

        [BindProperty]
        public List<IFormFile> UploadedImages { get; set; } = new List<IFormFile>();

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

        // Removed manual EventId input – database will assign identity

        public List<string> AvailableCategories { get; } = new List<string> 
        { 
            "Music", 
            "Sports", 
            "Arts", 
            "Food", 
            "Business", 
            "Education", 
            "Social", 
            "Other" 
        };

        public void OnGet()
        {
            _logger.LogInformation("Create event page loaded at {time}", DateTime.UtcNow);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("=== EVENT CREATION STARTED ===");
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("ModelState error: {Error}", error.ErrorMessage);
                }
                return Page();
            }

            try
            {
                _logger.LogInformation("Event data received:");
                _logger.LogInformation("Title: {Title}", Event.title);
                _logger.LogInformation("Location: {Location}", Event.location);
                _logger.LogInformation("Date: {Date}", Event.date);
                _logger.LogInformation("Price: {Price}", Event.price);
                _logger.LogInformation("Description: {Description}", Event.description?.Substring(0, Math.Min(50, Event.description.Length)) + "...");

                // Initialize collections if null
                if (Event.images == null)
                {
                    Event.images = new List<string>();
                }

                // Set default values
                Event.CreatedAt = DateTime.UtcNow;
                Event.UpdatedAt = DateTime.UtcNow;
                Event.Status = "Active";
                Event.Category = EventCategory ?? "Other";

                // Process uploaded images
                if (UploadedImages != null && UploadedImages.Count > 0)
                {
                    _logger.LogInformation("Processing {ImageCount} uploaded images", UploadedImages.Count);
                    
                    foreach (var image in UploadedImages.Take(3))
                    {
                        if (image.Length > 0)
                        {
                            try
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    await image.CopyToAsync(memoryStream);
                                    var bytes = memoryStream.ToArray();
                                    var base64 = Convert.ToBase64String(bytes);
                                    var dataUrl = $"data:{image.ContentType};base64,{base64}";
                                    Event.images.Add(dataUrl);
                                    _logger.LogInformation("Stored image as data URL: {FileName}", image.FileName);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Failed to store image: {FileName}, Error: {Error}", image.FileName, ex.Message);
                            }
                        }
                    }
                }

                // Add event to database
                _logger.LogInformation("Adding event to database context");
                var entry = _context.Events.Add(Event);
                
                _logger.LogInformation("Saving changes to database");
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync completed. Result: {Result}", saveResult);

                // Update the link after the event has an ID
                Event.link = $"/EventDetail?location={Event.location}&id={Event.id}";
                await _context.SaveChangesAsync();

                // Create audit log
                var auditLog = new AuditLog
                {
                    EntityName = "Event",
                    EntityId = Event.id.ToString(),
                    Action = "Create",
                    UserId = User.Identity?.Name ?? "System",
                    Changes = $"Created new event: {Event.title} (ID: {Event.id}) in location: {Event.location}",
                    Timestamp = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Event created successfully: {EventTitle} (ID: {EventId})", Event.title, Event.id);
                
                TempData["SuccessMessage"] = $"Event '{Event.title}' created successfully!";
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event: {Title}. Error: {Error}", 
                    Event.title, ex.Message);
                
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the event: {errorMessage}");
                return Page();
            }
        }
    }
} 
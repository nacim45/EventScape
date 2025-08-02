using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        [BindProperty]
        public string ImageUrls { get; set; }

        [BindProperty]
        public List<IFormFile> UploadedImages { get; set; } = new List<IFormFile>();

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

        public void OnGet()
        {
            _logger.LogInformation("Create event page loaded at {time}", DateTime.UtcNow);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state when creating event");
                    return Page();
                }

                // Begin transaction
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Process the form data and ensure location is lowercase for consistency
                    if (!string.IsNullOrEmpty(Event.location))
                    {
                        Event.location = Event.location.ToLower().Trim();
                    }

                    // Set additional event properties
                    Event.Category = EventCategory ?? "Other";
                    Event.Capacity = EventCapacity;
                    Event.StartTime = EventStartTime;
                    Event.EndTime = EventEndTime;
                    Event.Tags = EventTags;
                    Event.Status = "Active";
                    Event.CreatedAt = DateTime.UtcNow;
                    Event.UpdatedAt = DateTime.UtcNow;
                    Event.CreatedById = User.Identity?.Name;

                    // Initialize images list
                    Event.images = new List<string>();

                    // Process uploaded images
                    if (UploadedImages != null && UploadedImages.Count > 0)
                    {
                        string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "events");
                        
                        // Ensure directory exists
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Process up to 3 images
                        foreach (var image in UploadedImages.Take(3))
                        {
                            if (image.Length > 0)
                            {
                                // Create unique filename
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                                string filePath = Path.Combine(uploadsFolder, fileName);
                                
                                // Save image
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(stream);
                                }
                                
                                // Add relative path to event images
                                Event.images.Add($"images/events/{fileName}");
                                
                                _logger.LogInformation("Uploaded image {FileName} for event", fileName);
                            }
                        }
                    }

                    // Process image URLs if any
                    if (!string.IsNullOrWhiteSpace(ImageUrls))
                    {
                        var urls = ImageUrls
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(url => url.Trim())
                            .Where(url => !string.IsNullOrWhiteSpace(url))
                            .ToList();
                        
                        // Add URLs to event images
                        Event.images.AddRange(urls);
                    }

                    // Format the date correctly
                    if (!string.IsNullOrEmpty(Event.date) && DateTime.TryParse(Event.date, out DateTime parsedDate))
                    {
                        Event.date = parsedDate.ToString("dddd, dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    }

                    // Add event to database
                    await _context.Events.AddAsync(Event);
                    await _context.SaveChangesAsync();

                    // Create audit log entry
                    var auditLog = new AuditLog
                    {
                        EntityName = "Event",
                        EntityId = Event.id.ToString(),
                        Action = "Create",
                        UserId = User.Identity?.Name,
                        Changes = $"Created new event: {Event.title} (ID: {Event.id}) in location: {Event.location}",
                        Timestamp = DateTime.UtcNow
                    };
                    await _context.AuditLogs.AddAsync(auditLog);
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Event created successfully: {EventTitle} (ID: {EventId})", Event.title, Event.id);
                    TempData["SuccessMessage"] = $"Event '{Event.title}' created successfully!";
                    
                    // Redirect to Admin page
                    return RedirectToPage("/Admin");
                }
                catch (Exception ex)
                {
                    // Rollback transaction on error
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event: {Title}. Error: {Error}", 
                    Event.title, ex.Message);
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the event: {ex.Message}");
                return Page();
            }
        }
    }
} 
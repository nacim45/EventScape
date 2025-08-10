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
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("ModelState error: {Error}", error.ErrorMessage);
                }
                return Page();
            }

            _logger.LogInformation("Event data received:");
            _logger.LogInformation("Title: {Title}", Event.title);
            _logger.LogInformation("Location: {Location}", Event.location);
            _logger.LogInformation("Date: {Date}", Event.date);
            _logger.LogInformation("Price: {Price}", Event.price);
            _logger.LogInformation("Description: {Description}", Event.description?.Substring(0, Math.Min(50, Event.description.Length)) + "...");
            _logger.LogInformation("Category: {Category}", Event.Category);
            _logger.LogInformation("UploadedImages count: {Count}", UploadedImages?.Count ?? 0);

            // Initialize collections if null
            if (Event.images == null)
            {
                Event.images = new List<string>();
                _logger.LogInformation("Initialized Event.images collection");
            }

            // Check if location exists
            var existingLocation = await _context.Events
                .Where(e => e.location.ToLower() == Event.location.ToLower())
                .FirstOrDefaultAsync();
            
            bool isNewLocation = existingLocation == null;
            _logger.LogInformation("Is new location: {IsNewLocation}", isNewLocation);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Database transaction started");

                // Process uploaded images
                if (UploadedImages != null && UploadedImages.Count > 0)
                {
                    _logger.LogInformation("Processing {ImageCount} uploaded images", UploadedImages.Count);
                    
                    string uploadsFolder = Path.Combine(_environment.WebRootPath ?? throw new InvalidOperationException("WebRootPath is null"), "images", "events");
                
                    try
                    {
                        // Ensure directory exists
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                            _logger.LogInformation("Created events images directory: {Directory}", uploadsFolder);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Could not create events directory. Using data URL storage only. Error: {Error}", ex.Message);
                        uploadsFolder = null;
                    }

                    // Process up to 3 images
                    foreach (var image in UploadedImages.Take(3))
                    {
                        if (image.Length > 0)
                        {
                            _logger.LogInformation("Processing image: {FileName}, Size: {Size} bytes, ContentType: {ContentType}", 
                                image.FileName, image.Length, image.ContentType);
                            
                            try
                            {
                                if (uploadsFolder != null)
                                {
                                    // Create unique filename with original extension
                                    string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                                    if (string.IsNullOrEmpty(extension))
                                    {
                                        // Default to .jpg if no extension
                                        extension = ".jpg";
                                    }
                                    
                                    string fileName = Guid.NewGuid().ToString() + extension;
                                    string filePath = Path.Combine(uploadsFolder, fileName);
                                    
                                    // Save image to disk
                                    using (var stream = new FileStream(filePath, FileMode.Create))
                                    {
                                        await image.CopyToAsync(stream);
                                    }
                                    
                                    // Add relative path to event images
                                    Event.images.Add($"images/events/{fileName}");
                                    _logger.LogInformation("Successfully saved image {FileName} to disk", fileName);
                                }
                                else
                                {
                                    // Store as data URL if file system is not available
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
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("Could not save image to disk. Storing as data URL instead. Error: {Error}", ex.Message);
                                // Fallback to data URL storage
                                try
                                {
                                    using (var memoryStream = new MemoryStream())
                                    {
                                        await image.CopyToAsync(memoryStream);
                                        var bytes = memoryStream.ToArray();
                                        var base64 = Convert.ToBase64String(bytes);
                                        var dataUrl = $"data:{image.ContentType};base64,{base64}";
                                        Event.images.Add(dataUrl);
                                        _logger.LogInformation("Stored image as data URL after disk failure: {FileName}", image.FileName);
                                    }
                                }
                                catch (Exception dataUrlEx)
                                {
                                    _logger.LogError("Failed to store image as data URL: {FileName}, Error: {Error}", image.FileName, dataUrlEx.Message);
                                }
                            }
                        }
                    }
                    
                    _logger.LogInformation("Completed processing {ImageCount} images. Total images in event: {TotalImages}", 
                        UploadedImages.Count, Event.images.Count);
                }

                // Add event to database
                _logger.LogInformation("Attempting to save event to database: {Title}", Event.title);
                _logger.LogInformation("Event details before save: Location: {Location}, Date: {Date}, Price: {Price}", 
                    Event.location, Event.date, Event.price);
                
                var entry = await _context.Events.AddAsync(Event);
                _logger.LogInformation("Event added to context. Entry state: {State}", entry.State);
                
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync completed. Result: {Result}", saveResult);
                
                // Now the event has an ID, update the link and create audit log entry
                Event.link = $"/EventDetail?location={Event.location}&id={Event.id}";
                _logger.LogInformation("Updated event link: {Link}", Event.link);
                
                var updateResult = await _context.SaveChangesAsync();
                _logger.LogInformation("Second SaveChangesAsync completed. Result: {Result}", updateResult);
                
                var auditLog = new AuditLog
                {
                    EntityName = "Event",
                    EntityId = Event.id.ToString(),
                    Action = "Create",
                    UserId = User.Identity?.Name ?? "System",
                    Changes = $"Created new event: {Event.title} (ID: {Event.id}) in location: {Event.location}",
                    Timestamp = DateTime.UtcNow
                };
                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();
                
                // Commit transaction
                await transaction.CommitAsync();
                _logger.LogInformation("Database transaction committed successfully");
                
                _logger.LogInformation("Event created successfully: {EventTitle} (ID: {EventId})", Event.title, Event.id);
                
                var successMessage = isNewLocation
                    ? $"Event '{Event.title}' created successfully with new location '{Event.location}'! The event will be displayed in the Events page."
                    : $"Event '{Event.title}' created successfully in {Event.location}! The event will be displayed in both the Events page and the {Event.location} section.";
                
                TempData["SuccessMessage"] = successMessage;
                
                // Redirect to Events Index page
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during transaction: {Error}", ex.Message);
                
                // Get the inner exception message if available
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                
                // Log detailed error information
                _logger.LogError("Detailed error information:");
                _logger.LogError("Title: {Title}", Event.title);
                _logger.LogError("Location: {Location}", Event.location);
                _logger.LogError("Date: {Date}", Event.date);
                _logger.LogError("Category: {Category}", Event.Category);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
                
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the event: {errorMessage}");
                return Page();
            }
        }
    }
} 
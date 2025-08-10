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

                // Validate required fields
                if (string.IsNullOrEmpty(Event.title))
                {
                    ModelState.AddModelError("Event.title", "Title is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(Event.location))
                {
                    ModelState.AddModelError("Event.location", "Location is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(Event.description))
                {
                    ModelState.AddModelError("Event.description", "Description is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(Event.date))
                {
                    ModelState.AddModelError("Event.date", "Date is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(Event.price))
                {
                    ModelState.AddModelError("Event.price", "Price is required");
                    return Page();
                }

                // Validate description length
                if (Event.description.Length < 10)
                {
                    ModelState.AddModelError("Event.description", "Description must be at least 10 characters");
                    return Page();
                }

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
                Event.date = eventDate.ToString("dddd, dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

                // Process event images
                var imageUrls = new List<string>();
                if (UploadedImages != null && UploadedImages.Count > 0)
                {
                    _logger.LogInformation("Processing {ImageCount} uploaded images", UploadedImages.Count);
                    
                    // Only process up to 3 images
                    foreach (var image in UploadedImages.Take(3))
                    {
                        if (image.Length > 0)
                        {
                            try
                            {
                                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "events");
                                
                                // Create directory if it doesn't exist
                                if (!Directory.Exists(uploadsFolder))
                                {
                                    Directory.CreateDirectory(uploadsFolder);
                                }
                                
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(fileStream);
                                }
                                
                                imageUrls.Add("images/events/" + uniqueFileName);
                                _logger.LogInformation("Successfully saved image: {FileName}", uniqueFileName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Failed to save image: {FileName}, Error: {Error}", image.FileName, ex.Message);
                                // Continue with other images
                            }
                        }
                    }
                }

                // Get current user ID
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError(string.Empty, "User not found. Please log in again.");
                    return Page();
                }

                // Create new event with all fields
                var newEvent = new TheEvent
                {
                    title = Event.title.Trim(),
                    location = Event.location.ToLower().Trim(), // Store location in lowercase for consistency
                    date = Event.date,
                    description = Event.description.Trim(),
                    price = Event.price.Trim(),
                    images = imageUrls,
                    Category = EventCategory ?? "Other",
                    Capacity = EventCapacity,
                    StartTime = EventStartTime,
                    EndTime = EventEndTime,
                    Tags = EventTags,
                    Status = "Active",
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Validate time format if provided
                if (!string.IsNullOrEmpty(newEvent.StartTime) && !TimeSpan.TryParse(newEvent.StartTime, out _))
                {
                    ModelState.AddModelError("EventStartTime", "Invalid start time format");
                    return Page();
                }
                if (!string.IsNullOrEmpty(newEvent.EndTime) && !TimeSpan.TryParse(newEvent.EndTime, out _))
                {
                    ModelState.AddModelError("EventEndTime", "Invalid end time format");
                    return Page();
                }

                // Validate capacity
                if (newEvent.Capacity.HasValue && newEvent.Capacity.Value <= 0)
                {
                    ModelState.AddModelError("EventCapacity", "Capacity must be greater than 0");
                    return Page();
                }

                try
                {
                    // Begin transaction
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Add to database
                        await _context.Events.AddAsync(newEvent);
                        
                        // Save changes to get the event ID
                        await _context.SaveChangesAsync();

                        // Create audit log entry with the generated ID
                        var auditLog = new AuditLog
                        {
                            EntityName = "Event",
                            EntityId = newEvent.id.ToString(),
                            Action = "Create",
                            UserId = userId,
                            Changes = $"Created new event: {newEvent.title} (ID: {newEvent.id}) in location: {newEvent.location}",
                            Timestamp = DateTime.UtcNow
                        };
                        await _context.AuditLogs.AddAsync(auditLog);

                        // Save audit log
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        // Log success
                        _logger.LogInformation("Successfully created event: {EventId} - {EventTitle} in {Location}", 
                            newEvent.id, newEvent.title, newEvent.location);

                        TempData["SuccessMessage"] = $"Event '{newEvent.title}' created successfully!";
                        return RedirectToPage("./Index");
                    }
                    catch
                    {
                        // Rollback transaction on error
                        await transaction.RollbackAsync();
                        throw; // Re-throw to be caught by outer try-catch
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create event: {Title} in {Location}. Error: {Error}", 
                        newEvent.title, newEvent.location, ex.Message);
                    throw; // Re-throw to be handled by the calling method
                }
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
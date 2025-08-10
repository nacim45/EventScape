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

                    // Process the form data and ensure location is lowercase for consistency
                    Event.location = Event.location.ToLower().Trim();
                    Event.title = Event.title.Trim();
                    Event.description = Event.description.Trim();

                    // Log the location being used
                    _logger.LogInformation("Creating event with location: {Location}", Event.location);

                    // Check if this is a new location
                    var existingLocations = await _context.Events
                        .Where(e => !e.IsDeleted)
                        .Select(e => e.location.ToLower())
                        .Distinct()
                        .ToListAsync();

                    var isNewLocation = !existingLocations.Contains(Event.location);
                    _logger.LogInformation("Location {Location} is {Status}", Event.location, isNewLocation ? "new" : "existing");

                    // Validate and format date
                    if (DateTime.TryParse(Event.date, out DateTime parsedDate))
                    {
                        if (parsedDate < DateTime.Today)
                        {
                            ModelState.AddModelError("Event.date", "Event date must be in the future");
                            return Page();
                        }
                        Event.date = parsedDate.ToString("dddd, dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        ModelState.AddModelError("Event.date", "Invalid date format");
                        return Page();
                    }

                    // Format and validate price
                    if (!string.IsNullOrEmpty(Event.price))
                    {
                        var cleanPrice = Event.price.Trim();
                        if (cleanPrice.Equals("Free", StringComparison.OrdinalIgnoreCase))
                        {
                            Event.price = "Free";
                        }
                        else
                        {
                            var priceWithoutSymbol = cleanPrice.Replace("£", "").Trim();
                            if (decimal.TryParse(priceWithoutSymbol, out var priceValue))
                            {
                                Event.price = $"£{priceValue}";
                            }
                            else
                            {
                                ModelState.AddModelError("Event.price", "Price must be a number (e.g., £10 or 10) or 'Free'");
                                return Page();
                            }
                        }
                    }

                    // Set additional event properties
                    Event.Category = EventCategory ?? "Other";
                    Event.Capacity = EventCapacity;
                    Event.StartTime = EventStartTime;
                    Event.EndTime = EventEndTime;
                    Event.Tags = EventTags;
                    Event.Status = "Active";
                    var now = DateTime.UtcNow;
                    Event.CreatedAt = now;
                    Event.UpdatedAt = now;
                    _logger.LogInformation("Setting CreatedAt to: {CreatedAt}", now);
                    
                    // Get the current user's ID for the foreign key
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        _logger.LogError("Could not determine user ID for event creation");
                        ModelState.AddModelError(string.Empty, "Could not determine user ID. Please try logging out and back in.");
                        return Page();
                    }
                    Event.CreatedById = userId;
                    Event.IsDeleted = false;

                // Initialize images list
                Event.images = new List<string>();

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

                    // External image URLs removed for simplicity as requested

                    // Let database generate the ID
                    
                    // Add event to database
                    _logger.LogInformation("Attempting to save event to database: {Title}", Event.title);
                    _logger.LogInformation("Event details before save: Location: {Location}, Date: {Date}, Price: {Price}", 
                        Event.location, Event.date, Event.price);
                    
                    var entry = await _context.Events.AddAsync(Event);
                    await _context.SaveChangesAsync();
                    
                    // Now the event has an ID, update the link and create audit log entry
                    Event.link = $"/EventDetail?location={Event.location}&id={Event.id}";
                    await _context.SaveChangesAsync();
                    
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
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event: {Title}. Error: {Error}", 
                    Event.title, ex.Message);
                
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
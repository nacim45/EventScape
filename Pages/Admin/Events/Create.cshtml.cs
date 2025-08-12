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
using Microsoft.AspNetCore.Identity;

namespace soft20181_starter.Pages.Admin.Events
{
    [Authorize(Roles = "Administrator")]
    public class CreateModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<CreateModel> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(
            EventAppDbContext context, 
            ILogger<CreateModel> logger, 
            IWebHostEnvironment environment,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _userManager = userManager;
        }

        [BindProperty]
        public TheEvent Event { get; set; } = new TheEvent();

        [BindProperty]
        public string? ImageUrls { get; set; }

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
                if (string.IsNullOrEmpty(Event.title?.Trim()))
                {
                    ModelState.AddModelError("Event.title", "Title is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(Event.location?.Trim()))
                {
                    ModelState.AddModelError("Event.location", "Location is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(Event.description?.Trim()))
                {
                    ModelState.AddModelError("Event.description", "Description is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(Event.date))
                {
                    ModelState.AddModelError("Event.date", "Date is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(Event.price?.Trim()))
                {
                    ModelState.AddModelError("Event.price", "Price is required");
                    return Page();
                }

                // Validate description length
                if (Event.description.Trim().Length < 10)
                {
                    ModelState.AddModelError("Event.description", "Description must be at least 10 characters");
                    return Page();
                }

                // Enhanced price formatting and validation
                var originalPrice = Event.price.Trim();
                var formattedPrice = originalPrice;
                
                // Handle different price formats
                if (originalPrice.Equals("Free", StringComparison.OrdinalIgnoreCase) || 
                    originalPrice.Equals("0", StringComparison.OrdinalIgnoreCase))
                {
                    formattedPrice = "Free";
                }
                else
                {
                    // Remove £ symbol if present and validate numeric
                    var priceWithoutSymbol = originalPrice.Replace("£", "").Replace("$", "").Trim();
                    
                    if (decimal.TryParse(priceWithoutSymbol, out decimal priceValue))
                    {
                        // Format as £price for display
                        formattedPrice = $"£{priceValue:F2}";
                    }
                    else
                    {
                        ModelState.AddModelError("Event.price", "Price must be a valid number, 'Free', or start with £");
                        return Page();
                    }
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

                // Process event images with enhanced error handling and UI integration
                var imageUrls = new List<string>();
                if (UploadedImages != null && UploadedImages.Count > 0)
                {
                    _logger.LogInformation("Processing {ImageCount} uploaded images for UI integration", UploadedImages.Count);
                    
                    // Only process up to 3 images for optimal UI display
                    foreach (var image in UploadedImages.Take(3))
                    {
                        if (image.Length > 0)
                        {
                            try
                            {
                                // Generate unique filename for database storage
                                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "events");
                                
                                // Create directory if it doesn't exist
                                if (!Directory.Exists(uploadsFolder))
                                {
                                    Directory.CreateDirectory(uploadsFolder);
                                    _logger.LogInformation("Created events directory: {Directory}", uploadsFolder);
                                }
                                
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                
                                // Save image to file system for UI display
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(fileStream);
                                }
                                
                                // Add to imageUrls list for database storage and UI integration
                                var imageUrl = "images/events/" + uniqueFileName;
                                imageUrls.Add(imageUrl);
                                _logger.LogInformation("Successfully integrated image: {FileName} -> {ImageUrl} for UI display", uniqueFileName, imageUrl);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Failed to integrate image: {FileName}, Error: {Error}", image.FileName, ex.Message);
                                // Continue with other images
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No images uploaded - event will be created without images");
                }

                // Get current user ID
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found. Please log in again.");
                    return Page();
                }

                var userId = currentUser.Id;
                _logger.LogInformation("Creating event for user: {UserId} ({UserName})", userId, currentUser.UserName);

                // Create new event with all fields
                var newEvent = new TheEvent
                {
                    title = Event.title.Trim(),
                    location = Event.location.ToLower().Trim(), // Store location in lowercase for consistency
                    date = Event.date,
                    description = Event.description.Trim(),
                    price = formattedPrice, // Use formatted price
                    images = imageUrls,
                    Category = EventCategory ?? "Other",
                    Capacity = EventCapacity,
                    StartTime = EventStartTime,
                    EndTime = EventEndTime,
                    Tags = EventTags,
                    Status = "Active",
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
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
                    // Begin transaction for atomic database operations
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _logger.LogInformation("=== DATABASE INSERT OPERATION STARTED ===");
                        _logger.LogInformation("Form data to be inserted into Events table:");
                        _logger.LogInformation("- Title: {Title}", newEvent.title);
                        _logger.LogInformation("- Location: {Location}", newEvent.location);
                        _logger.LogInformation("- Date: {Date}", newEvent.date);
                        _logger.LogInformation("- Price: {Price}", newEvent.price);
                        _logger.LogInformation("- Description: {Description}", newEvent.description?.Substring(0, Math.Min(50, newEvent.description.Length)) + "...");
                        _logger.LogInformation("- Images: {ImageCount} images", newEvent.images.Count);
                        _logger.LogInformation("- CreatedBy: {UserId}", newEvent.CreatedById);
                        _logger.LogInformation("- Category: {Category}", newEvent.Category);
                        _logger.LogInformation("- Capacity: {Capacity}", newEvent.Capacity);
                        _logger.LogInformation("- Status: {Status}", newEvent.Status);
                        _logger.LogInformation("- StartTime: {StartTime}", newEvent.StartTime);
                        _logger.LogInformation("- EndTime: {EndTime}", newEvent.EndTime);
                        _logger.LogInformation("- Tags: {Tags}", newEvent.Tags);

                        // Add event to database context - this prepares the INSERT statement
                        await _context.Events.AddAsync(newEvent);
                        _logger.LogInformation("Event added to DbContext - preparing INSERT INTO Events table");

                        // Execute the INSERT query - this actually inserts the record into the database
                        var saveResult = await _context.SaveChangesAsync();
                        _logger.LogInformation("INSERT INTO Events table completed successfully. Rows affected: {RowsAffected}", saveResult);

                        // Log the generated event ID from the database
                        _logger.LogInformation("Event record inserted with auto-generated ID: {EventId}", newEvent.id);

                        // Create audit log entry with the generated ID
                        var auditLog = new AuditLog
                        {
                            EntityName = "Event",
                            EntityId = newEvent.id.ToString(),
                            Action = "Create",
                            UserId = userId,
                            Changes = $"INSERT INTO Events: {newEvent.title} (ID: {newEvent.id}) in location: {newEvent.location} with price: {newEvent.price} and {newEvent.images.Count} images",
                            Timestamp = DateTime.UtcNow
                        };
                        
                        await _context.AuditLogs.AddAsync(auditLog);
                        var auditSaveResult = await _context.SaveChangesAsync();
                        _logger.LogInformation("INSERT INTO AuditLogs table completed. Rows affected: {RowsAffected}", auditSaveResult);

                        // Commit transaction
                        await transaction.CommitAsync();
                        _logger.LogInformation("=== DATABASE TRANSACTION COMMITTED SUCCESSFULLY ===");

                        // Log success with database confirmation
                        _logger.LogInformation("Database INSERT operations completed successfully:");
                        _logger.LogInformation("- Event record inserted into Events table with ID: {EventId}", newEvent.id);
                        _logger.LogInformation("- Audit log record inserted into AuditLogs table");
                        _logger.LogInformation("- All form data successfully persisted to database");
                        
                        TempData["SuccessMessage"] = $"Event '{newEvent.title}' created successfully with ID: {newEvent.id}! Database INSERT completed.";
                        return RedirectToPage("/Admin/Events/Index");
                    }
                    catch (Exception dbEx)
                    {
                        // Rollback transaction on error
                        await transaction.RollbackAsync();
                        _logger.LogError(dbEx, "Database INSERT operation failed - transaction rolled back. Error: {Error}", dbEx.Message);
                        throw; // Re-throw to be caught by outer try-catch
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute INSERT operation for event: {Title} in {Location}. Error: {Error}", newEvent.title, newEvent.location, ex.Message);
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
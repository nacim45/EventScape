using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using soft20181_starter.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace soft20181_starter.Pages
{
    [Authorize(Roles = "Admin,Administrator")]
    public class AdminModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AdminModel> _logger;

        public AdminModel(
            EventAppDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ILogger<AdminModel> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            StatusMessage = string.Empty;
            NewEvent = new TheEvent();
            Events = new List<TheEvent>();
            Users = new List<AppUser>();
        }

        [TempData]
        public string StatusMessage { get; set; }

        public List<TheEvent> Events { get; set; }
        public List<AppUser> Users { get; set; }

        [BindProperty]
        public TheEvent NewEvent { get; set; }

        public IActionResult OnGet()
        {
            // Check if user is authenticated and has admin role
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login", new { returnUrl = "/Admin" });
            }
            
            if (!User.IsInRole("Admin") && !User.IsInRole("Administrator"))
            {
                TempData["AdminAccessDenied"] = "Only administrators can access the Admin Interface.";
                return RedirectToPage("/Index");
            }

            // Get all events
            Events = _context.Events.ToList();

            // Get all users
            Users = _context.Users.ToList();

            return Page();
        }

        // Add Event Handler
        public async Task<IActionResult> OnPostAddEventAsync(List<IFormFile> Images)
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Please check your input and try again.";
                return RedirectToPage();
            }

            try
            {
                // Process event images
                var imageUrls = new List<string>();
                if (Images != null && Images.Count > 0)
                {
                    // Only process up to 3 images
                    foreach (var image in Images.Take(3))
                    {
                        if (image.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "events");
                            
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
                        }
                    }
                }

                // Get current user ID
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User not found");
                }

                // Create new event with all fields
                var newEvent = new TheEvent
                {
                    title = NewEvent.title,
                    location = NewEvent.location.ToLower(), // Store location in lowercase for consistency
                    date = NewEvent.date,
                    description = NewEvent.description,
                    price = NewEvent.price,
                    images = imageUrls,
                    Category = NewEvent.Category,
                    Capacity = NewEvent.Capacity,
                    StartTime = NewEvent.StartTime,
                    EndTime = NewEvent.EndTime,
                    Tags = NewEvent.Tags,
                    Status = "Active",
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Validate date format
                if (!DateTime.TryParse(newEvent.date, out _))
                {
                    throw new ArgumentException("Invalid date format");
                }

                // Validate time format if provided
                if (!string.IsNullOrEmpty(newEvent.StartTime) && !TimeSpan.TryParse(newEvent.StartTime, out _))
                {
                    throw new ArgumentException("Invalid start time format");
                }
                if (!string.IsNullOrEmpty(newEvent.EndTime) && !TimeSpan.TryParse(newEvent.EndTime, out _))
                {
                    throw new ArgumentException("Invalid end time format");
                }

                // Validate capacity
                if (newEvent.Capacity.HasValue && newEvent.Capacity.Value <= 0)
                {
                    throw new ArgumentException("Capacity must be greater than 0");
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

                StatusMessage = "Event added successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                // Log the error
                _logger.LogError(ex, "Error adding event");
            }

            return RedirectToPage();
        }

        // Edit Event Handler
        public async Task<IActionResult> OnPostEditEventAsync(TheEvent EditEvent, List<IFormFile> EditImages, bool ReplaceImages = false)
        {
            if (EditEvent == null || EditEvent.id <= 0)
            {
                StatusMessage = "Error: Invalid event data.";
                return RedirectToPage();
            }

            try
            {
                var existingEvent = await _context.Events
                    .Include(e => e.Attendances)
                    .FirstOrDefaultAsync(e => e.id == EditEvent.id);

                if (existingEvent == null)
                {
                    StatusMessage = "Error: Event not found.";
                    return RedirectToPage();
                }

                // Get current user ID
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User not found");
                }

                // Store original values for audit log
                var originalValues = new
                {
                    existingEvent.title,
                    existingEvent.location,
                    existingEvent.date,
                    existingEvent.description,
                    existingEvent.price,
                    existingEvent.Category,
                    existingEvent.Capacity,
                    existingEvent.Status
                };

                // Update event properties
                existingEvent.title = EditEvent.title;
                existingEvent.location = EditEvent.location.ToLower();
                existingEvent.date = EditEvent.date;
                existingEvent.description = EditEvent.description;
                existingEvent.price = EditEvent.price;
                existingEvent.Category = EditEvent.Category;
                existingEvent.Capacity = EditEvent.Capacity;
                existingEvent.StartTime = EditEvent.StartTime;
                existingEvent.EndTime = EditEvent.EndTime;
                existingEvent.Tags = EditEvent.Tags;
                existingEvent.Status = EditEvent.Status;
                existingEvent.UpdatedAt = DateTime.UtcNow;

                // Validate date format
                if (!DateTime.TryParse(existingEvent.date, out _))
                {
                    throw new ArgumentException("Invalid date format");
                }

                // Validate time format if provided
                if (!string.IsNullOrEmpty(existingEvent.StartTime) && !TimeSpan.TryParse(existingEvent.StartTime, out _))
                {
                    throw new ArgumentException("Invalid start time format");
                }
                if (!string.IsNullOrEmpty(existingEvent.EndTime) && !TimeSpan.TryParse(existingEvent.EndTime, out _))
                {
                    throw new ArgumentException("Invalid end time format");
                }

                // Validate capacity
                if (existingEvent.Capacity.HasValue)
                {
                    if (existingEvent.Capacity.Value <= 0)
                    {
                        throw new ArgumentException("Capacity must be greater than 0");
                    }
                    if (existingEvent.Capacity.Value < existingEvent.Attendances.Count)
                    {
                        throw new ArgumentException("Cannot set capacity lower than current number of attendees");
                    }
                }

                // Handle images
                if (EditImages != null && EditImages.Count > 0)
                {
                    var imageUrls = new List<string>();
                    
                    // Process new images
                    foreach (var image in EditImages.Take(3))
                    {
                        if (image.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "events");
                            
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
                        }
                    }

                    if (ReplaceImages)
                    {
                        // Delete old image files
                        foreach (var oldImage in existingEvent.images ?? new List<string>())
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, oldImage.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                        // Replace existing images with new ones
                        existingEvent.images = imageUrls;
                    }
                    else
                    {
                        // Add new images to existing ones (up to 3 total)
                        var currentImages = existingEvent.images ?? new List<string>();
                        var combinedImages = currentImages.Concat(imageUrls).Take(3).ToList();
                        existingEvent.images = combinedImages;
                    }
                }

                // Create audit log entry
                var changes = new List<string>();
                if (originalValues.title != existingEvent.title) changes.Add($"Title: {originalValues.title} -> {existingEvent.title}");
                if (originalValues.location != existingEvent.location) changes.Add($"Location: {originalValues.location} -> {existingEvent.location}");
                if (originalValues.date != existingEvent.date) changes.Add($"Date: {originalValues.date} -> {existingEvent.date}");
                if (originalValues.price != existingEvent.price) changes.Add($"Price: {originalValues.price} -> {existingEvent.price}");
                if (originalValues.Category != existingEvent.Category) changes.Add($"Category: {originalValues.Category} -> {existingEvent.Category}");
                if (originalValues.Capacity != existingEvent.Capacity) changes.Add($"Capacity: {originalValues.Capacity} -> {existingEvent.Capacity}");
                if (originalValues.Status != existingEvent.Status) changes.Add($"Status: {originalValues.Status} -> {existingEvent.Status}");

                var auditLog = new AuditLog
                {
                    EntityName = "Event",
                    EntityId = existingEvent.id.ToString(),
                    Action = "Update",
                    UserId = userId,
                    Changes = string.Join(", ", changes),
                    Timestamp = DateTime.UtcNow
                };
                await _context.AuditLogs.AddAsync(auditLog);

                try
                {
                    // Begin transaction
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Save changes first to update the event
                        await _context.SaveChangesAsync();

                        // Save audit log
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        // Log success
                        _logger.LogInformation(
                            "Successfully updated event: {EventId} - {EventTitle} in {Location}. Changes: {Changes}", 
                            existingEvent.id, 
                            existingEvent.title, 
                            existingEvent.location,
                            auditLog.Changes
                        );
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
                    _logger.LogError(ex, "Failed to update event: {EventId} - {Title}. Error: {Error}", 
                        existingEvent.id, existingEvent.title, ex.Message);
                    throw; // Re-throw to be handled by the calling method
                }

                StatusMessage = "Event updated successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error updating event {EventId}", EditEvent.id);
            }

            return RedirectToPage();
        }

        // Delete Event Handler
        public async Task<IActionResult> OnPostDeleteEventAsync(int eventId)
        {
            try
            {
                var eventToDelete = await _context.Events
                    .Include(e => e.Attendances)
                    .FirstOrDefaultAsync(e => e.id == eventId);

                if (eventToDelete == null)
                {
                    StatusMessage = "Error: Event not found.";
                    return RedirectToPage();
                }

                // Get current user ID
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User not found");
                }

                // Check if event has attendees
                if (eventToDelete.Attendances.Any())
                {
                    // Soft delete if there are attendees
                    eventToDelete.IsDeleted = true;
                    eventToDelete.Status = "Cancelled";
                    eventToDelete.UpdatedAt = DateTime.UtcNow;

                    // Create audit log entry for soft delete
                    var auditLog = new AuditLog
                    {
                        EntityName = "Event",
                        EntityId = eventToDelete.id.ToString(),
                        Action = "SoftDelete",
                        UserId = userId,
                        Changes = $"Soft deleted event: {eventToDelete.title} (has {eventToDelete.Attendances.Count} attendees)",
                        Timestamp = DateTime.UtcNow
                    };
                    await _context.AuditLogs.AddAsync(auditLog);
                }
                else
                {
                    // Hard delete if no attendees
                    // Delete image files
                    foreach (var image in eventToDelete.images ?? new List<string>())
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    _context.Events.Remove(eventToDelete);

                    // Create audit log entry for hard delete
                    var auditLog = new AuditLog
                    {
                        EntityName = "Event",
                        EntityId = eventToDelete.id.ToString(),
                        Action = "HardDelete",
                        UserId = userId,
                        Changes = $"Hard deleted event: {eventToDelete.title}",
                        Timestamp = DateTime.UtcNow
                    };
                    await _context.AuditLogs.AddAsync(auditLog);
                }

                try
                {
                    // Begin transaction
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Save changes
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        // Log success
                        _logger.LogInformation(
                            "Successfully deleted event: {EventId} - {Title}. Type: {DeleteType}", 
                            eventToDelete.id, 
                            eventToDelete.title,
                            eventToDelete.Attendances.Any() ? "Soft Delete" : "Hard Delete"
                        );

                        StatusMessage = "Event deleted successfully!";
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
                    _logger.LogError(ex, "Failed to delete event: {EventId} - {Title}. Error: {Error}", 
                        eventToDelete.id, eventToDelete.title, ex.Message);
                    throw; // Re-throw to be handled by the calling method
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error deleting event {EventId}", eventId);
            }

            return RedirectToPage();
        }

        // Add User Handler
        public async Task<IActionResult> OnPostAddUserAsync(string Name, string Surname, string Email, string Password, string Role)
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Surname) || 
                string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || 
                string.IsNullOrEmpty(Role))
            {
                StatusMessage = "Error: All fields are required.";
                return RedirectToPage();
            }

            try
            {
                // Check if user already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
                if (existingUser != null)
                {
                    StatusMessage = "Error: A user with this email already exists.";
                    return RedirectToPage();
                }

                // Create new user - using AppUser for Identity
                var newUser = new AppUser
                {
                    UserName = Email, // Identity requires UserName
                    Name = Name,
                    Surname = Surname,
                    Email = Email,
                    Password = Password, // In a real app, this would be hashed
                    Role = Role
                };

                // Add the user to the context
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                StatusMessage = "User added successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Edit User Handler
        public async Task<IActionResult> OnPostEditUserAsync(string Id, string Name, string Surname, string Email, string Password, string Role)
        {
            if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Surname) || 
                string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Role))
            {
                StatusMessage = "Error: Invalid user data.";
                return RedirectToPage();
            }

            try
            {
                var user = await _context.Users.FindAsync(Id);

                if (user == null)
                {
                    StatusMessage = "Error: User not found.";
                    return RedirectToPage();
                }

                // Update user properties
                user.Name = Name;
                user.Surname = Surname;
                user.Email = Email;
                user.UserName = Email; // Keep username and email in sync
                user.Role = Role;

                // Update password if provided
                if (!string.IsNullOrEmpty(Password))
                {
                    user.Password = Password;
                }

                await _context.SaveChangesAsync();
                StatusMessage = "User updated successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Delete User Handler
        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    StatusMessage = "Error: User not found.";
                    return RedirectToPage();
                }

                // Check if trying to delete current user
                if (User.Identity != null && user.Email == User.Identity.Name)
                {
                    StatusMessage = "Error: You cannot delete your own account.";
                    return RedirectToPage();
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                StatusMessage = "User deleted successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
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

namespace soft20181_starter.Pages
{
    [Authorize(Roles = "Admin,Administrator")]
    public class AdminModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminModel(EventAppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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

                // Create new event
                var newEvent = new TheEvent
                {
                    title = NewEvent.title,
                    location = NewEvent.location,
                    date = NewEvent.date,
                    description = NewEvent.description,
                    price = NewEvent.price,
                    images = imageUrls
                };

                _context.Events.Add(newEvent);
                await _context.SaveChangesAsync();

                StatusMessage = "Event added successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
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
                var existingEvent = _context.Events.Find(EditEvent.id);

                if (existingEvent == null)
                {
                    StatusMessage = "Error: Event not found.";
                    return RedirectToPage();
                }

                // Update event properties
                existingEvent.title = EditEvent.title;
                existingEvent.location = EditEvent.location;
                existingEvent.date = EditEvent.date;
                existingEvent.description = EditEvent.description;
                existingEvent.price = EditEvent.price;

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

                await _context.SaveChangesAsync();
                StatusMessage = "Event updated successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Delete Event Handler
        public async Task<IActionResult> OnPostDeleteEventAsync(int eventId)
        {
            try
            {
                var eventToDelete = _context.Events.Find(eventId);

                if (eventToDelete == null)
                {
                    StatusMessage = "Error: Event not found.";
                    return RedirectToPage();
                }

                _context.Events.Remove(eventToDelete);
                await _context.SaveChangesAsync();

                StatusMessage = "Event deleted successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
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
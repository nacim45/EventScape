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

                // Process the form data and capitalize the first letter of location
                if (!string.IsNullOrEmpty(Event.location))
                {
                    // Ensure first letter is capitalized for location
                    Event.location = char.ToUpper(Event.location[0]) + Event.location.Substring(1).ToLower();
                }

                // Save the additional event properties to the ViewData for display purpose
                // Since we're not modifying the TheEvent model, we'll store these as TempData
                if (!string.IsNullOrEmpty(EventCategory))
                {
                    TempData["EventCategory"] = EventCategory;
                }

                if (EventCapacity.HasValue)
                {
                    TempData["EventCapacity"] = EventCapacity.Value;
                }

                if (!string.IsNullOrEmpty(EventStartTime))
                {
                    TempData["EventStartTime"] = EventStartTime;
                }

                if (!string.IsNullOrEmpty(EventEndTime))
                {
                    TempData["EventEndTime"] = EventEndTime;
                }

                if (!string.IsNullOrEmpty(EventTags))
                {
                    TempData["EventTags"] = EventTags;
                }

                // Initialize images list
                Event.images = new List<string>();

                // Process uploaded images
                if (UploadedImages != null && UploadedImages.Count > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "events");
                    
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
                            Event.images.Add($"uploads/events/{fileName}");
                            
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

                // Format the date correctly if it's not already in the right format
                if (!string.IsNullOrEmpty(Event.date) && DateTime.TryParse(Event.date, out DateTime parsedDate))
                {
                    Event.date = parsedDate.ToString("yyyy-MM-dd");
                }

                // Add event to database
                _context.Events.Add(Event);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Event created successfully: {EventTitle}", Event.title);
                TempData["SuccessMessage"] = "Event created successfully!";
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the event. Please try again.");
                return Page();
            }
        }
    }
} 
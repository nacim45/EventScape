using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace soft20181_starter.Pages
{
    public class EventDetailsModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<EventDetailsModel> _logger;
        private readonly Random _random = new Random();

        public EventDetailsModel(EventAppDbContext context, ILogger<EventDetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public TheEvent? Event { get; set; }
        public List<string> Locations { get; set; } = new List<string>();
        public string ErrorMessage { get; set; } = string.Empty;
        public List<RelatedEventModel> RelatedEvents { get; set; } = new List<RelatedEventModel>();
        public string SuccessMessage { get; set; } = string.Empty;
        public bool IsAlreadyRegistered { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(string location, int? id)
        {
            try
            {
                _logger.LogInformation("Accessing EventDetails with location: {Location}, id: {Id}", location, id);

                // Load available locations
                Locations = await _context.Events
                    .Select(e => e.location)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();

                // If no ID is provided, show random events
                if (id == null)
                {
                    _logger.LogInformation("No event ID provided, showing random events");
                    await LoadRandomEvents();
                    return Page();
                }

                // If location is provided, use it in query
                if (!string.IsNullOrEmpty(location))
                {
                    Event = await _context.Events.FirstOrDefaultAsync(e => e.location.ToLower() == location.ToLower() && e.id == id);
                }
                else
                {
                    // If no location provided, just query by ID
                    Event = await _context.Events.FirstOrDefaultAsync(e => e.id == id);
                }

                if (Event == null)
                {
                    _logger.LogWarning("Event not found with location: {Location}, id: {Id}", location, id);
                    ErrorMessage = "The requested event could not be found.";
                    await LoadRandomEvents();
                    return Page();
                }

                // Check if user is already registered for this event
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    IsAlreadyRegistered = await _context.EventAttendances
                        .AnyAsync(ea => ea.EventId == Event.id && ea.UserId == userId && ea.Status != "Cancelled");
                }

                // Load related events from the same location (excluding the current event)
                var events = await _context.Events
                    .Where(e => e.location.ToLower() == Event.location.ToLower() && e.id != Event.id && !e.IsDeleted)
                    .Take(12)
                    .ToListAsync();

                // Convert to related event models safely
                RelatedEvents = new List<RelatedEventModel>();
                foreach (var e in events)
                {
                    try
                    {
                        decimal price = 0;
                        string priceStr = e.price.Replace("£", "").Replace("$", "").Trim();
                        
                        // Try to parse the price, defaulting to 0 if it fails
                        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                        {
                            price = 0;
                        }

                        // Try to parse the date, defaulting to current date if it fails
                        DateTime date;
                        if (!DateTime.TryParse(e.date, out date))
                        {
                            date = DateTime.Now;
                        }

                        RelatedEvents.Add(new RelatedEventModel
                        {
                            Id = e.id,
                            Title = e.title,
                            Location = char.ToUpper(e.location[0]) + e.location.Substring(1),
                            Date = date,
                            Price = price,
                            ImageUrl = e.images != null && e.images.Any() ? $"/{e.images[0]}" : "/images/placeholder.png"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error creating related event model: {Error}", ex.Message);
                        // Continue to the next event if there's an error with this one
                    }
                }

                _logger.LogInformation("Event found: {Title}", Event.title);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event details: {Message}", ex.Message);
                ErrorMessage = "An error occurred while loading the event details.";
                await LoadRandomEvents();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRegisterForEventAsync(int eventId)
        {
            _logger.LogInformation("Attempting to register for event ID: {EventId}", eventId);

            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Unauthenticated user attempted to register for event");
                    return RedirectToPage("/Account/Login");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Verify the user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User with ID {UserId} not found during event registration", userId);
                    ErrorMessage = "Unable to register for event: User not found in the database.";
                    return RedirectToPage("/EventDetail", new { id = eventId, error = "user-not-found" });
                }
                
                // Get the event
                var theEvent = await _context.Events
                    .Where(e => !e.IsDeleted)
                    .FirstOrDefaultAsync(e => e.id == eventId);
                    
                if (theEvent == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found during registration", eventId);
                    return NotFound("Event not found or has been deleted.");
                }
                
                // Check if user is already registered for this event
                var existingAttendance = await _context.EventAttendances
                    .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId);

                if (existingAttendance != null)
                {
                    if (existingAttendance.Status == "Cancelled")
                    {
                        // Reactivate registration if previously cancelled
                        existingAttendance.Status = "Registered";
                        existingAttendance.RegisteredDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("User {UserId} re-registered for event {EventId}", userId, eventId);
                        
                        // Reload the page with success message
                        return RedirectToPage("/EventDetail", new { id = eventId, registrationSuccess = true });
                    }
                    else
                    {
                        // User is already registered
                        _logger.LogInformation("User {UserId} is already registered for event {EventId}", userId, eventId);
                        
                        // Reload the page with already registered message
                        return RedirectToPage("/EventDetail", new { id = eventId, alreadyRegistered = true });
                    }
                }

                // Generate a unique ticket number
                string ticketNumber = GenerateTicketNumber(userId, eventId);

                // Create new attendance record
                var attendance = new EventAttendance
                {
                    UserId = userId,
                    EventId = eventId,
                    RegisteredDate = DateTime.Now,
                    TicketNumber = ticketNumber,
                    Status = "Registered"
                };

                _context.EventAttendances.Add(attendance);
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User {UserId} successfully registered for event {EventId}", userId, eventId);
                    
                    // Redirect to MyEvents page to show their registrations
                    return RedirectToPage("/MyEvents");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database error while registering user {UserId} for event {EventId}", userId, eventId);
                    
                    // Handle foreign key constraint issues
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY constraint failed"))
                    {
                        _logger.LogError("Foreign key constraint failed. Attempting to verify the EventAttendances table structure");
                        
                        // Attempt to fix the EventAttendances table
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(@"
                            DROP TABLE IF EXISTS EventAttendances;
                            CREATE TABLE EventAttendances (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                UserId TEXT NOT NULL,
                                EventId INTEGER NOT NULL,
                                RegisteredDate TEXT NOT NULL,
                                TicketNumber TEXT NULL,
                                Status TEXT NOT NULL,
                                FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
                                FOREIGN KEY (EventId) REFERENCES Events (id) ON DELETE CASCADE
                            );
                            CREATE INDEX IX_EventAttendances_UserId_EventId ON EventAttendances (UserId, EventId);
                            ");
                            
                            _logger.LogInformation("EventAttendances table recreated. Redirecting user to try again");
                            return RedirectToPage("/EventDetail", new { id = eventId, error = "database-fixed" });
                        }
                        catch (Exception fixEx)
                        {
                            _logger.LogError(fixEx, "Failed to recreate EventAttendances table: {Message}", fixEx.Message);
                            ErrorMessage = "Unable to register for event: Database constraint violation. Please contact an administrator.";
                        }
                    }
                    
                    return RedirectToPage("/EventDetail", new { id = eventId, error = "foreign-key-constraint" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during event registration: {Message}", ex.Message);
                ErrorMessage = "An unexpected error occurred during registration.";
                return RedirectToPage("/EventDetail", new { id = eventId, error = "general-error" });
            }
        }

        private string GenerateTicketNumber(string userId, int eventId)
        {
            // Create a ticket number format: EVT-{eventId}-{userId first 4 chars}-{timestamp}
            string userPart = userId.Length > 4 ? userId.Substring(0, 4) : userId;
            string timestamp = DateTime.Now.ToString("yyMMddHHmm");
            return $"EVT-{eventId}-{userPart}-{timestamp}";
        }

        private async Task LoadRandomEvents()
        {
            try
            {
                // Get all events
                var allEvents = await _context.Events
                    .Where(e => !e.IsDeleted)
                    .ToListAsync();

                if (!allEvents.Any())
                {
                    return;
                }

                // Organize events by location
                var eventsByLocation = allEvents
                    .GroupBy(e => e.location.ToLower())
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Try to get events from different locations
                var randomEvents = new List<TheEvent>();
                var locationsUsed = new HashSet<string>();
                
                // First pass - try to get one event from each location
                foreach (var locationGroup in eventsByLocation)
                {
                    if (randomEvents.Count >= 15) break;
                    
                    var location = locationGroup.Key;
                    var eventsInLocation = locationGroup.Value;
                    
                    if (eventsInLocation.Any())
                    {
                        var randomEvent = eventsInLocation[_random.Next(eventsInLocation.Count)];
                        randomEvents.Add(randomEvent);
                        locationsUsed.Add(location);
                    }
                }
                
                // Second pass - fill remaining slots with random events
                while (randomEvents.Count < 15 && allEvents.Count > randomEvents.Count)
                {
                    var remainingEvents = allEvents.Except(randomEvents).ToList();
                    if (!remainingEvents.Any()) break;
                    
                    var randomEvent = remainingEvents[_random.Next(remainingEvents.Count)];
                    randomEvents.Add(randomEvent);
                }

                // Convert to related event models
                RelatedEvents = new List<RelatedEventModel>();
                foreach (var e in randomEvents)
                {
                    try
                    {
                        decimal price = 0;
                        string priceStr = e.price.Replace("£", "").Replace("$", "").Trim();
                        
                        // Try to parse the price, defaulting to 0 if it fails
                        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                        {
                            price = 0;
                        }

                        // Try to parse the date, defaulting to current date if it fails
                        DateTime date;
                        if (!DateTime.TryParse(e.date, out date))
                        {
                            date = DateTime.Now;
                        }

                        RelatedEvents.Add(new RelatedEventModel
                        {
                            Id = e.id,
                            Title = e.title,
                            Location = char.ToUpper(e.location[0]) + e.location.Substring(1),
                            Date = date,
                            Price = price,
                            ImageUrl = e.images != null && e.images.Any() ? $"/{e.images[0]}" : "/images/placeholder.png"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error creating random event model: {Error}", ex.Message);
                        // Continue to the next event if there's an error with this one
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading random events: {Message}", ex.Message);
            }
        }
    }

    // Model for related events display
    public class RelatedEventModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
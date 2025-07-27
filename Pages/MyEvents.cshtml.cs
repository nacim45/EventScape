using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace soft20181_starter.Pages
{
    [Authorize]
    public class MyEventsModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<MyEventsModel> _logger;
        private readonly UserManager<AppUser> _userManager;

        public MyEventsModel(EventAppDbContext context, ILogger<MyEventsModel> logger, UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            UserAttendingEvents = new List<EventAttendance>();
        }

        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public List<EventAttendance> UserAttendingEvents { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Page();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found for authenticated user");
                return Page();
            }

            try
            {
                // Get user details
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    UserName = user.UserName ?? string.Empty;
                    UserEmail = user.Email ?? string.Empty;
                }

                // Get all events the user is attending with proper includes
                UserAttendingEvents = await _context.EventAttendances
                    .Include(ea => ea.Event)
                    .Include(ea => ea.User)
                    .Where(ea => ea.UserId == userId && ea.Status != "Cancelled")
                    .OrderBy(ea => ea.Event.date)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} events for user {UserId}", 
                    UserAttendingEvents.Count, userId);

                // Log any potential issues with the data
                foreach (var attendance in UserAttendingEvents)
                {
                    if (attendance.Event == null)
                    {
                        _logger.LogWarning("Event is null for attendance {AttendanceId}", attendance.Id);
                    }
                    if (string.IsNullOrEmpty(attendance.TicketNumber))
                    {
                        _logger.LogWarning("Ticket number is missing for attendance {AttendanceId}", attendance.Id);
                        // Generate a ticket number if missing
                        attendance.TicketNumber = $"TKT-{attendance.Id:D6}";
                        _context.EventAttendances.Update(attendance);
                    }
                }

                // Save any changes made to ticket numbers
                await _context.SaveChangesAsync();

                // If no events found, check if there's an issue with the database
                if (UserAttendingEvents.Count == 0)
                {
                    var totalAttendances = await _context.EventAttendances.CountAsync();
                    _logger.LogInformation("Total attendances in database: {Count}", totalAttendances);
                    
                    var userAttendances = await _context.EventAttendances
                        .Where(ea => ea.UserId == userId)
                        .CountAsync();
                    _logger.LogInformation("Total attendances for user {UserId}: {Count}", userId, userAttendances);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event attendances: {Message}", ex.Message);
                UserAttendingEvents = new List<EventAttendance>();
                
                if (ex.Message.Contains("no such table") || ex.InnerException?.Message.Contains("no such table") == true)
                {
                    TempData["ErrorMessage"] = "The event attendances table doesn't exist in the database. Please contact support.";
                }
                else
                {
                    TempData["ErrorMessage"] = "An error occurred while retrieving your events. Please try again later.";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAttendanceAsync(int attendanceId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            try
            {
                var attendance = await _context.EventAttendances
                    .FirstOrDefaultAsync(ea => ea.Id == attendanceId && ea.UserId == userId);

                if (attendance == null)
                {
                    return NotFound();
                }

                // Instead of deleting, mark as cancelled
                attendance.Status = "Cancelled";
                await _context.SaveChangesAsync();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling attendance: {Message}", ex.Message);
                // Check if this is a table not found error
                if (ex.Message.Contains("no such table") || ex.InnerException?.Message.Contains("no such table") == true)
                {
                    TempData["ErrorMessage"] = "The event attendances table doesn't exist in the database. You need to recreate the database schema.";
                }
                else
                {
                    TempData["ErrorMessage"] = "An error occurred while cancelling your attendance.";
                }
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPost(int eventId, bool isAttending)
        {
            _logger.LogInformation("MyEvents POST request at {time} for EventId: {EventId}, IsAttending: {IsAttending}", 
                DateTime.UtcNow, eventId, isAttending);

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthenticated user attempted POST on MyEvents");
                return RedirectToPage("/Account/Login");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            try
            {
                var attendance = await _context.EventAttendances
                    .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId);

                if (attendance == null)
                {
                    return NotFound();
                }

                attendance.Status = isAttending ? "Attending" : "Not Attending";
                await _context.SaveChangesAsync();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance: {Message}", ex.Message);
                // Check if this is a table not found error
                if (ex.Message.Contains("no such table") || ex.InnerException?.Message.Contains("no such table") == true)
                {
                    TempData["ErrorMessage"] = "The event attendances table doesn't exist in the database. You need to recreate the database schema.";
                }
                else
                {
                    TempData["ErrorMessage"] = "An error occurred while updating your attendance status.";
                }
                return RedirectToPage();
            }
        }
    }
} 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Events
{
    [Authorize(Roles = "Administrator")]
    public class DetailsModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(EventAppDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public TheEvent Event { get; set; } = new TheEvent();
        public List<EventAttendance> Attendees { get; set; } = new List<EventAttendance>();
        public int AttendeeCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                _logger.LogInformation("Loading event details for ID: {EventId}", id);

                // Load the event with all its details
                Event = await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.id == id);

                if (Event == null)
                {
                    _logger.LogWarning("Event with ID {EventId} not found", id);
                    return Page();
                }

                // Load event attendees
                Attendees = await _context.EventAttendances
                    .Include(ea => ea.User)
                    .Where(ea => ea.EventId == id)
                    .OrderByDescending(ea => ea.RegisteredDate)
                    .ToListAsync();

                AttendeeCount = Attendees.Count;
                
                _logger.LogInformation("Found event {EventTitle} with {AttendeeCount} attendees", 
                    Event.title, AttendeeCount);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event details for ID: {EventId}", id);
                return RedirectToPage("./Index");
            }
        }
    }
} 
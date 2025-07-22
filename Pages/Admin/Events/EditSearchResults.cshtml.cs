using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Events
{
    [Authorize(Roles = "Administrator,Admin")]
    public class EditSearchResultsModel : PageModel
    {
        private readonly ILogger<EditSearchResultsModel> _logger;
        private readonly EventAppDbContext _context;

        public List<TheEvent> Events { get; set; } = new List<TheEvent>();
        public bool HasSearchResults => Events != null && Events.Any();

        public EditSearchResultsModel(ILogger<EditSearchResultsModel> logger, EventAppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(string eventName, string location)
        {
            if (string.IsNullOrWhiteSpace(eventName) && string.IsNullOrWhiteSpace(location))
            {
                _logger.LogWarning("Search for events to edit with no criteria provided");
                return RedirectToPage("SearchForEdit");
            }

            _logger.LogInformation("Searching for events to edit with Name: {EventName}, Location: {Location}", 
                eventName ?? "(not specified)", location ?? "(not specified)");

            // Query the database directly using DbContext
            var query = _context.Events.AsQueryable();

            // Apply filters if provided - using case-insensitive comparisons
            if (!string.IsNullOrWhiteSpace(eventName))
            {
                query = query.Where(e => EF.Functions.Like(e.title, $"%{eventName}%"));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(e => EF.Functions.Like(e.location, $"%{location}%"));
            }

            // Execute query and get results
            Events = await query.ToListAsync();

            _logger.LogInformation("Found {Count} events matching the search criteria", Events.Count);

            return Page();
        }
    }
} 
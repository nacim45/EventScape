using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages
{
    public class EventsModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<EventsModel> _logger;

        public EventsModel(EventAppDbContext context, ILogger<EventsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string Location { get; set; } = string.Empty;

        public List<TheEvent> Events { get; set; } = new List<TheEvent>();
        public List<string> AvailableLocations { get; set; } = new List<string>();
        public int TotalEvents { get; set; }

        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalPages { get; set; }

        // Quick search properties
        [BindProperty]
        public string QuickSearchInput { get; set; } = string.Empty;
        public string QuickSearchErrorMessage { get; set; } = string.Empty;

        public List<string> Locations { get; set; } = new List<string>();
        public async Task OnGetAsync(string? location = null, string? searchString = null, int currentPage = 1)
        {
            try
            {
                _logger.LogInformation("Events page accessed. Location: {Location}, Search: {Search}, Page: {Page}",
                    location, searchString, currentPage);

                // Store search parameters
                Location = location ?? string.Empty;
                SearchString = searchString ?? string.Empty;
                CurrentPage = Math.Max(1, currentPage);

                // Get all available locations from the database
                AvailableLocations = await _context.Events
                    .Where(e => !e.IsDeleted) // Only get locations from non-deleted events
                    .Select(e => e.location.ToLower()) // Convert to lowercase
                    .Distinct()
                    .Where(l => !string.IsNullOrEmpty(l))
                    .OrderBy(l => l)
                    .ToListAsync();

                // Initialize query
                var query = _context.Events
                    .AsQueryable();

                // Filter out soft-deleted events first
                query = query.Where(e => !e.IsDeleted);

                // Filter by location if provided
                if (!string.IsNullOrEmpty(Location))
                {
                    var locationLower = Location.ToLower();
                    query = query.Where(e => e.location.ToLower() == locationLower);
                }

                // Filter by search string if provided
                if (!string.IsNullOrEmpty(SearchString))
                {
                    var searchLower = SearchString.ToLower();
                    query = query.Where(e => 
                        e.title.ToLower().Contains(searchLower) || 
                        e.description.ToLower().Contains(searchLower) ||
                        e.location.ToLower().Contains(searchLower) ||
                        e.Category.ToLower().Contains(searchLower) ||
                        (e.Tags != null && e.Tags.ToLower().Contains(searchLower))
                    );
                }

                // Get total count for pagination
                var totalEvents = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(totalEvents / (double)PageSize);

                // Adjust current page if it's out of range
                if (TotalPages > 0 && CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                }

                // Get the events for the current page
                Events = await query
                    .OrderByDescending(e => e.CreatedAt) // Newest events first
                    .ThenByDescending(e => e.id)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Format dates and locations for display
                foreach (var evt in Events)
                {
                    if (DateTime.TryParse(evt.date, out DateTime parsedDate))
                    {
                        evt.date = parsedDate.ToString("dddd, dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (!string.IsNullOrEmpty(evt.location))
                    {
                        evt.location = char.ToUpper(evt.location[0]) + evt.location.Substring(1);
                    }
                }

                _logger.LogInformation("Retrieved {Count} events for page {Page} of {TotalPages}",
                    Events.Count, CurrentPage, TotalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading events page");
                Events = new List<TheEvent>();
                TotalPages = 0;
            }
        }

        public async Task<IActionResult> OnPostQuickSearchAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(QuickSearchInput))
                {
                    QuickSearchErrorMessage = "Please enter an event name to search";
                    await LoadAvailableLocationsAsync();
                    return Page();
                }

                _logger.LogInformation("Quick search for event: {EventName}", QuickSearchInput);

                // Find an event matching the name (case insensitive)
                var matchingEvent = await _context.Events
                    .Where(e => !e.IsDeleted)
                    .FirstOrDefaultAsync(e => e.title.ToLower() == QuickSearchInput.ToLower());

                if (matchingEvent != null)
                {
                    _logger.LogInformation("Found matching event: {EventId} - {EventTitle}", matchingEvent.id, matchingEvent.title);
                    return RedirectToPage("/EventDetail", new { id = matchingEvent.id });
                }
                else
                {
                    _logger.LogInformation("No exact match found for: {EventName}", QuickSearchInput);
                    QuickSearchErrorMessage = $"No event found with the name '{QuickSearchInput}'";
                    
                    // Load available locations and events for the page
                    await LoadAvailableLocationsAsync();
                    await OnGetAsync(Location, SearchString, CurrentPage);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during quick search: {Message}", ex.Message);
                QuickSearchErrorMessage = "An error occurred while searching for the event";
                await LoadAvailableLocationsAsync();
                return Page();
            }
        }

        private async Task LoadAvailableLocationsAsync()
        {
            // Get all available locations from non-deleted events
            AvailableLocations = await _context.Events
                .Where(e => !e.IsDeleted)
                .Select(e => e.location.ToLower())
                .Distinct()
                .Where(l => !string.IsNullOrEmpty(l))
                .OrderBy(l => l)
                .ToListAsync();
        }
    }
}
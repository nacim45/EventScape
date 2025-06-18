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
    public class IndexModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(EventAppDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; } = string.Empty;

        public List<TheEvent> Events { get; set; } = new List<TheEvent>();

        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading Admin Events page. Search: {Search}, Page: {Page}",
                    SearchString, CurrentPage);

                // Initialize query
                var query = _context.Events
                    .AsQueryable();

                // Apply search filter if specified
                if (!string.IsNullOrEmpty(SearchString))
                {
                    query = query.Where(e =>
                        e.title.Contains(SearchString) ||
                        e.location.Contains(SearchString) ||
                        e.description.Contains(SearchString)
                    );
                    _logger.LogInformation("Filtered by search: {Search}", SearchString);
                }

                // Calculate total pages for pagination
                var totalEvents = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(totalEvents / (double)PageSize);

                // Ensure current page is within valid range
                if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }
                else if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    CurrentPage = TotalPages;
                }

                // Get paginated results
                Events = await query
                    .OrderBy(e => e.title)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                _logger.LogInformation("Returning {Count} events for page {Page} of {TotalPages}",
                    Events.Count, CurrentPage, TotalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin events page");
                Events = new List<TheEvent>();
                TotalPages = 0;
            }
        }
    }
} 
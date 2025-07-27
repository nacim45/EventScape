using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;

namespace soft20181_starter.Pages.Admin.Events
{
    [Authorize(Roles = "Administrator,Admin")]
    public class SearchForEditModel : PageModel
    {
        private readonly ILogger<SearchForEditModel> _logger;

        [BindProperty]
        public string EventName { get; set; } = string.Empty;

        [BindProperty]
        public string Location { get; set; } = string.Empty;

        public SearchForEditModel(ILogger<SearchForEditModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Search for event to edit page displayed at {Time}", 
                DateTime.UtcNow);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if at least one search parameter is provided
            if (string.IsNullOrWhiteSpace(EventName) && string.IsNullOrWhiteSpace(Location))
            {
                ModelState.AddModelError(string.Empty, "Please provide at least one search parameter.");
                return Page();
            }

            _logger.LogInformation("Searching for event with Name: {EventName}, Location: {Location}", 
                EventName, Location);

            // Redirect to search results page with query parameters
            return RedirectToPage("EditSearchResults", new { eventName = EventName, location = Location });
        }
    }
} 
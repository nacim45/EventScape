using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator,Admin")]
    public class SearchForEditModel : PageModel
    {
        private readonly ILogger<SearchForEditModel> _logger;

        public SearchForEditModel(ILogger<SearchForEditModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Surname { get; set; }

        public void OnGet()
        {
            _logger.LogInformation("Search for Edit page displayed at {Time}", DateTime.Now);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _logger.LogInformation("Searching for user with Name: {Name}, Surname: {Surname}", 
                Name ?? "(empty)", Surname ?? "(empty)");

            return RedirectToPage("./EditSearchResults", new { Name = Name, Surname = Surname });
        }
    }
} 
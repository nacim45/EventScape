using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator,Admin")]
    public class SearchForDeleteModel : PageModel
    {
        private readonly ILogger<SearchForDeleteModel> _logger;

        public SearchForDeleteModel(ILogger<SearchForDeleteModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Surname { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public void OnGet()
        {
            _logger.LogInformation("Search for Delete page displayed at {Time}", DateTime.Now);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _logger.LogInformation("Searching for user to delete with Name: {Name}, Surname: {Surname}", 
                Name ?? "(empty)", Surname ?? "(empty)");

            return RedirectToPage("./DeleteSearchResults", new { searchName = Name, searchSurname = Surname });
        }
    }
} 
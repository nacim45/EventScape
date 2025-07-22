using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using soft20181_starter.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator,Admin")]
    public class EditSearchResultsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EditSearchResultsModel> _logger;

        public EditSearchResultsModel(
            UserManager<AppUser> userManager,
            ILogger<EditSearchResultsModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Name { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string Surname { get; set; } = string.Empty;

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Surname))
            {
                _logger.LogInformation("Search results page accessed without search parameters at {Time}", DateTime.Now);
                return RedirectToPage("./SearchForEdit");
            }

            _logger.LogInformation("Searching for users with Name: {Name}, Surname: {Surname}", 
                Name ?? "(empty)", Surname ?? "(empty)");

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                query = query.Where(u => u.Name.Contains(Name));
            }

            if (!string.IsNullOrWhiteSpace(Surname))
            {
                query = query.Where(u => u.Surname.Contains(Surname));
            }

            var users = await Task.FromResult(query.ToList());
            
            Users = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                Users.Add(new UserViewModel
                {
                    Id = user.Id ?? string.Empty,
                    Name = user.Name ?? string.Empty,
                    Surname = user.Surname ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    RegisteredDate = user.RegisteredDate,
                    Roles = roles?.ToList() ?? new List<string>()
                });
            }

            _logger.LogInformation("Found {Count} users matching search criteria", Users.Count);

            return Page();
        }
    }
} 
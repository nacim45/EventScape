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
    public class DeleteSearchResultsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DeleteSearchResultsModel> _logger;

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchSurname { get; set; }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        public bool HasResults => Users != null && Users.Count > 0;

        public DeleteSearchResultsModel(
            UserManager<AppUser> userManager,
            ILogger<DeleteSearchResultsModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(SearchName) && string.IsNullOrEmpty(SearchSurname))
            {
                _logger.LogInformation("No search parameters provided. Redirecting to search page.");
                return RedirectToPage("./SearchForDelete");
            }

            _logger.LogInformation("Searching for users with Name: {Name}, Surname: {Surname}", 
                string.IsNullOrEmpty(SearchName) ? "(any)" : SearchName, 
                string.IsNullOrEmpty(SearchSurname) ? "(any)" : SearchSurname);

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(SearchName))
            {
                query = query.Where(u => u.Name.Contains(SearchName));
            }

            if (!string.IsNullOrEmpty(SearchSurname))
            {
                query = query.Where(u => u.Surname.Contains(SearchSurname));
            }

            var results = await Task.FromResult(query.OrderBy(u => u.Email).ThenBy(u => u.Name).ToList());

            Users = results.Select(u => new UserViewModel
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                RegisteredDate = u.RegisteredDate
            }).ToList();

            _logger.LogInformation("Found {Count} users matching the search criteria", Users.Count);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = "Error: No user ID provided.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                return Page();
            }

            try
            {
                // Make sure we're not deleting an administrator (optional safety check)
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Administrator") && !User.IsInRole("Administrator"))
                {
                    StatusMessage = "Error: Only administrators can delete administrator accounts.";
                    return RedirectToPage();
                }

                // Delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} was successfully deleted by admin", user.Email);
                    StatusMessage = "User was successfully deleted.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    StatusMessage = "Error: Unable to delete user. See details below.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                StatusMessage = $"An error occurred: {ex.Message}";
            }

            // If we get here, repopulate the search results
            return RedirectToPage();
        }
    }
} 
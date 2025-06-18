using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using soft20181_starter.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator,Admin")]
    public class DeleteModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DeleteModel> _logger;

        [BindProperty]
        public string UserId { get; set; } = string.Empty;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public string SearchName { get; set; } = string.Empty;
        public string SearchSurname { get; set; } = string.Empty;
        
        [ViewData]
        public new UserViewModel User { get; set; }

        public DeleteModel(
            UserManager<AppUser> userManager,
            ILogger<DeleteModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string id, string name, string surname)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./SearchForDelete");
            }

            UserId = id;
            SearchName = name;
            SearchSurname = surname;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                return Page();
            }

            var roles = await _userManager.GetRolesAsync(user);
            
            User = new UserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                RegisteredDate = user.RegisteredDate,
                Roles = roles.ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                return RedirectToPage("./Index");
            }

            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                return RedirectToPage("./Index");
            }

            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} deleted successfully.", UserId);
                    StatusMessage = "User was successfully deleted.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to delete user {UserId}. Errors: {Errors}", UserId, errors);
                    StatusMessage = $"Error: Failed to delete user. {errors}";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting user {UserId}", UserId);
                StatusMessage = "Error: An unexpected error occurred while deleting the user.";
                return RedirectToPage("./Index");
            }
        }
    }
} 
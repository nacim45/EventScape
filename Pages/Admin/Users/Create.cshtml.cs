using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator,Admin")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<CreateModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public UserCreateViewModel UserViewModel { get; set; }

        public List<string> AvailableRoles { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Initialize available roles
            AvailableRoles = new List<string>();
            foreach (var role in _roleManager.Roles)
            {
                AvailableRoles.Add(role.Name);
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Re-populate available roles in case of validation errors
            AvailableRoles = new List<string>();
            foreach (var role in _roleManager.Roles)
            {
                AvailableRoles.Add(role.Name);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new AppUser
            {
                UserName = UserViewModel.Email,
                Email = UserViewModel.Email,
                Name = UserViewModel.Name,
                Surname = UserViewModel.Surname,
                PhoneNumber = UserViewModel.PhoneNumber,
                RegisteredDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, UserViewModel.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account.");

                // Assign role if specified
                if (!string.IsNullOrEmpty(UserViewModel.Role))
                {
                    await _userManager.AddToRoleAsync(user, UserViewModel.Role);
                }

                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "User role is required")]
        public string Role { get; set; } = string.Empty;
    }
} 
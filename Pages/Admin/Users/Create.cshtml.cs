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
using System.Linq;

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
        public UserCreateViewModel UserViewModel { get; set; } = new UserCreateViewModel();

        public List<string> AvailableRoles { get; set; } = new List<string>();

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
            _logger.LogInformation("=== USER CREATION STARTED ===");
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            
            // Re-populate available roles in case of validation errors
            AvailableRoles = new List<string>();
            foreach (var role in _roleManager.Roles)
            {
                AvailableRoles.Add(role.Name ?? string.Empty);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("ModelState error: {Error}", error.ErrorMessage);
                }
                return Page();
            }

            _logger.LogInformation("User data received:");
            _logger.LogInformation("Name: {Name}", UserViewModel.Name);
            _logger.LogInformation("Surname: {Surname}", UserViewModel.Surname);
            _logger.LogInformation("Email: {Email}", UserViewModel.Email);
            _logger.LogInformation("Role: {Role}", UserViewModel.Role);
            _logger.LogInformation("Phone: {Phone}", UserViewModel.PhoneNumber);

            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(UserViewModel.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("User with email {Email} already exists", UserViewModel.Email);
                    ModelState.AddModelError(string.Empty, "A user with this email address already exists.");
                    return Page();
                }

                var user = new AppUser
                {
                    UserName = UserViewModel.Email,
                    Email = UserViewModel.Email,
                    Name = UserViewModel.Name,
                    Surname = UserViewModel.Surname,
                    PhoneNumber = UserViewModel.PhoneNumber ?? string.Empty,
                    RegisteredDate = DateTime.Now,
                    Role = string.IsNullOrWhiteSpace(UserViewModel.Role) ? "User" : UserViewModel.Role,
                    EmailConfirmed = true, // Auto-confirm email for admin-created users
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("AppUser object created with ID: {Id}", user.Id);
                _logger.LogInformation("User details: Name={Name}, Surname={Surname}, Email={Email}, Role={Role}", 
                    user.Name, user.Surname, user.Email, user.Role);

                var result = await _userManager.CreateAsync(user, UserViewModel.Password);
                _logger.LogInformation("UserManager.CreateAsync result: Succeeded={Succeeded}", result.Succeeded);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

                    // Assign role if specified
                    if (!string.IsNullOrEmpty(UserViewModel.Role))
                    {
                        _logger.LogInformation("Assigning role {Role} to user {UserId}", UserViewModel.Role, user.Id);
                        var roleResult = await _userManager.AddToRoleAsync(user, UserViewModel.Role);
                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation("Role {Role} assigned successfully to user {UserId}", UserViewModel.Role, user.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to assign role {Role} to user {UserId}", UserViewModel.Role, user.Id);
                            foreach (var error in roleResult.Errors)
                            {
                                _logger.LogWarning("Role assignment error: {Error}", error.Description);
                            }
                        }
                    }

                    TempData["SuccessMessage"] = $"User '{user.Name} {user.Surname}' created successfully with ID: {user.Id}.";
                    _logger.LogInformation("Redirecting to Index page with success message");
                    return RedirectToPage("./Index");
                }

                _logger.LogError("Failed to create user");
                foreach (var error in result.Errors)
                {
                    _logger.LogError("User creation error: {Error}", error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during user creation");
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the user: {ex.Message}");
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
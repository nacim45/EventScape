using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using soft20181_starter.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator,Admin")]
    public class EditModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<EditModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;
        
        public string StatusMessageClass { get; set; } = string.Empty;

        [BindProperty]
        public string UserId { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string ReturnSearchName { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string ReturnSearchSurname { get; set; } = string.Empty;

        [BindProperty]
        public UserEditViewModel UserEdit { get; set; }

        public SelectList Roles { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("User ID was not provided for editing");
                TempData["ErrorMessage"] = "User ID was not provided";
                return RedirectToPage("./Index");
            }

            UserId = id;
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToPage("./Index");
            }

            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault() ?? "";

            // Create the view model
            UserEdit = new UserEditViewModel
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = currentRole
            };

            // Load available roles for the dropdown
            LoadRoles();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found during update", UserId);
                TempData["ErrorMessage"] = "User not found";
                return RedirectToPage("./Index");
            }

            // Check if current user is trying to edit themselves and change their role
            var currentUser = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);
            var currentRole = userRoles.FirstOrDefault() ?? "";

            if (currentUser?.Id == user.Id && currentRole != UserEdit.Role)
            {
                _logger.LogWarning("User attempted to change their own role");
                TempData["ErrorMessage"] = "You cannot change your own role";
                LoadRoles();
                return Page();
            }

            // Update basic user properties
            user.Name = UserEdit.Name;
            user.Surname = UserEdit.Surname;
            user.Email = UserEdit.Email;
            user.PhoneNumber = UserEdit.PhoneNumber;
            user.UserName = UserEdit.Email; // Keep username synced with email

            // Try to update the user
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                LoadRoles();
                return Page();
            }

            // Handle role changes
            if (currentRole != UserEdit.Role)
            {
                // Remove existing roles
                if (!string.IsNullOrEmpty(currentRole))
                {
                    await _userManager.RemoveFromRoleAsync(user, currentRole);
                }

                // Add new role
                if (!string.IsNullOrEmpty(UserEdit.Role))
                {
                    await _userManager.AddToRoleAsync(user, UserEdit.Role);
                }
            }

            // Handle password change if provided
            if (!string.IsNullOrEmpty(UserEdit.Password))
            {
                // Remove the existing password
                await _userManager.RemovePasswordAsync(user);

                // Add the new password
                var passwordResult = await _userManager.AddPasswordAsync(user, UserEdit.Password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    LoadRoles();
                    return Page();
                }
            }

            _logger.LogInformation("User {UserId} updated successfully", UserId);
            TempData["SuccessMessage"] = "User updated successfully";
            return RedirectToPage("./Index");
        }

        private void LoadRoles()
        {
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            Roles = new SelectList(roles);
        }
    }

    public class UserEditViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Password")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;
    }
} 
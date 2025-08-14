using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using soft20181_starter.ViewModels;
using soft20181_starter.Services;
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
        private readonly SimpleAuditService _auditService;

        public EditModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<EditModel> logger,
            SimpleAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _auditService = auditService;
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

            try
            {
                _logger.LogInformation("=== USER UPDATE STARTED ===");
                _logger.LogInformation("Updating user with ID: {UserId}", UserId);

                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for update", UserId);
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToPage("/Admin/Users/Index");
                }

                // Update user properties
                user.Name = UserEdit.Name?.Trim() ?? string.Empty;
                user.Surname = UserEdit.Surname?.Trim() ?? string.Empty;
                user.Email = UserEdit.Email?.Trim() ?? string.Empty;
                user.UserName = UserEdit.Email?.Trim() ?? string.Empty;
                user.PhoneNumber = UserEdit.PhoneNumber?.Trim() ?? string.Empty;
                user.Role = UserEdit.Role ?? "User";
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = User.Identity?.Name ?? "System";

                // Update the user
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to update user {UserId}: {Errors}", UserId, errors);
                    ModelState.AddModelError(string.Empty, $"Failed to update user: {errors}");
                    LoadRoles();
                    return Page();
                }

                // Update user role if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentRole = currentRoles.FirstOrDefault() ?? "";
                
                if (currentRole != UserEdit.Role)
                {
                    // Remove current role
                    if (!string.IsNullOrEmpty(currentRole))
                    {
                        await _userManager.RemoveFromRoleAsync(user, currentRole);
                    }
                    
                    // Add new role
                    if (!string.IsNullOrEmpty(UserEdit.Role))
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, UserEdit.Role);
                        if (!roleResult.Succeeded)
                        {
                            _logger.LogWarning("Failed to assign role {Role} to user {UserId}", UserEdit.Role, UserId);
                        }
                    }
                }

                // Create audit log for user update
                try
                {
                    var oldValues = new Dictionary<string, object?>
                    {
                        ["Name"] = user.Name,
                        ["Surname"] = user.Surname,
                        ["Email"] = user.Email,
                        ["PhoneNumber"] = user.PhoneNumber,
                        ["Role"] = currentRole
                    };

                    var newValues = new Dictionary<string, object?>
                    {
                        ["Name"] = UserEdit.Name?.Trim() ?? string.Empty,
                        ["Surname"] = UserEdit.Surname?.Trim() ?? string.Empty,
                        ["Email"] = UserEdit.Email?.Trim() ?? string.Empty,
                        ["PhoneNumber"] = UserEdit.PhoneNumber?.Trim() ?? string.Empty,
                        ["Role"] = UserEdit.Role ?? "User"
                    };

                    await _auditService.LogUpdateAsync(user, oldValues, newValues);
                    _logger.LogInformation("Audit log created for user update {UserId}", UserId);
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning("Failed to create audit log for user update {UserId}: {Error}", UserId, auditEx.Message);
                    // Don't fail the entire operation if audit log fails
                }

                _logger.LogInformation("Successfully updated user: {UserId} - {Name} {Surname}", 
                    UserId, user.Name, user.Surname);

                TempData["SuccessMessage"] = $"User '{user.Name} {user.Surname}' updated successfully!";
                return RedirectToPage("/Admin/Users/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", UserId);
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the user: {ex.Message}");
                LoadRoles();
                return Page();
            }
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
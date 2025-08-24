using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using soft20181_starter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator,Admin")]
    public class DetailsModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DetailsModel> _logger;
        private readonly SimpleAuditService _auditService;

        public DetailsModel(
            EventAppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<DetailsModel> logger,
            SimpleAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _auditService = auditService;
        }

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Surname { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime RegisteredDate { get; set; }
            public List<string> UserRoles { get; set; } = new List<string>();
            public string CurrentRole { get; set; } = string.Empty;
        }

        public new UserViewModel User { get; set; } = new UserViewModel();
        public List<EventAttendance> EventAttendances { get; set; } = new List<EventAttendance>();
        public bool IsSuperAdmin { get; set; } = false;
        public bool CanUpgradeUser { get; set; } = false;
        public string CurrentUserRole { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                _logger.LogInformation("Loading user details for ID: {UserId}", id);

                // Get current user's role
                var currentUser = await _userManager.GetUserAsync(base.User);
                if (currentUser != null)
                {
                    var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
                    CurrentUserRole = currentUserRoles.FirstOrDefault() ?? "User";
                    IsSuperAdmin = CurrentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", id);
                    return NotFound();
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var currentRole = userRoles.FirstOrDefault() ?? "User";

                User = new UserViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    RegisteredDate = user.RegisteredDate,
                    UserRoles = userRoles.ToList(),
                    CurrentRole = currentRole
                };

                // Determine if current user can upgrade this user
                CanUpgradeUser = IsSuperAdmin && currentRole.Equals("User", StringComparison.OrdinalIgnoreCase);

                // Get event attendances
                EventAttendances = await _context.EventAttendances
                    .Include(ea => ea.Event)
                    .Where(ea => ea.UserId == id)
                    .ToListAsync();

                _logger.LogInformation("User details loaded successfully for {UserName}", $"{user.Name} {user.Surname}");

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while loading the user details.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpgradeToAdminAsync(string id)
        {
            try
            {
                _logger.LogInformation("Attempting to upgrade user {UserId} to Admin", id);

                // Get current user's role
                var currentUser = await _userManager.GetUserAsync(base.User);
                if (currentUser != null)
                {
                    var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
                    CurrentUserRole = currentUserRoles.FirstOrDefault() ?? "User";
                    IsSuperAdmin = CurrentUserRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
                }

                // Only SuperAdmin can upgrade users
                if (!IsSuperAdmin)
                {
                    _logger.LogWarning("Non-SuperAdmin user attempted to upgrade user {UserId} - denied", id);
                    StatusMessage = "Only SuperAdmin can upgrade users to Admin role.";
                    return RedirectToPage(new { id });
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for upgrade", id);
                    StatusMessage = "User not found.";
                    return RedirectToPage(new { id });
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var currentRole = userRoles.FirstOrDefault() ?? "User";

                // Only upgrade if user is currently a standard user
                if (!currentRole.Equals("User", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Attempted to upgrade user {UserId} with role {Role} - denied", id, currentRole);
                    StatusMessage = "Only standard users can be upgraded to Admin role.";
                    return RedirectToPage(new { id });
                }

                // Remove User role and add Admin role
                var removeResult = await _userManager.RemoveFromRoleAsync(user, "User");
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("Failed to remove User role from {UserId}: {Errors}", id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    StatusMessage = "Failed to remove current role.";
                    return RedirectToPage(new { id });
                }

                var addResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addResult.Succeeded)
                {
                    _logger.LogError("Failed to add Admin role to {UserId}: {Errors}", id, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    StatusMessage = "Failed to assign Admin role.";
                    return RedirectToPage(new { id });
                }

                // Update user's role in the database
                user.Role = "Admin";
                await _context.SaveChangesAsync();

                // Create audit log
                try
                {
                    await _auditService.LogUserActionAsync(
                        "User Role Upgrade",
                        $"User {user.Name} {user.Surname} ({user.Email}) upgraded from User to Admin role by SuperAdmin",
                        currentUser?.Id,
                        currentUser?.UserName
                    );
                    _logger.LogInformation("Audit log created for user upgrade {UserId}", id);
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning("Failed to create audit log for user upgrade {UserId}: {Error}", id, auditEx.Message);
                }

                _logger.LogInformation("User {UserId} successfully upgraded to Admin role", id);
                StatusMessage = $"User '{user.Name} {user.Surname}' has been successfully upgraded to Admin role.";

                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading user {UserId} to Admin", id);
                StatusMessage = "An error occurred while upgrading the user.";
                return RedirectToPage(new { id });
            }
        }
    }
} 
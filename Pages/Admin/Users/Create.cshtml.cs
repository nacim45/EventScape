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
using System.Text.RegularExpressions;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator,Admin")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EventAppDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            EventAppDbContext context,
            ILogger<CreateModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
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

            try
            {
                _logger.LogInformation("User data received:");
                _logger.LogInformation("Name: {Name}", UserViewModel.Name);
                _logger.LogInformation("Surname: {Surname}", UserViewModel.Surname);
                _logger.LogInformation("Email: {Email}", UserViewModel.Email);
                _logger.LogInformation("Role: {Role}", UserViewModel.Role);

                // Validate required fields
                if (string.IsNullOrEmpty(UserViewModel.Name?.Trim()))
                {
                    ModelState.AddModelError("UserViewModel.Name", "First name is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(UserViewModel.Surname?.Trim()))
                {
                    ModelState.AddModelError("UserViewModel.Surname", "Last name is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(UserViewModel.Email?.Trim()))
                {
                    ModelState.AddModelError("UserViewModel.Email", "Email is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(UserViewModel.Password))
                {
                    ModelState.AddModelError("UserViewModel.Password", "Password is required");
                    return Page();
                }
                if (string.IsNullOrEmpty(UserViewModel.ConfirmPassword))
                {
                    ModelState.AddModelError("UserViewModel.ConfirmPassword", "Please confirm your password");
                    return Page();
                }

                // Validate email format
                if (!Regex.IsMatch(UserViewModel.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError("UserViewModel.Email", "Please enter a valid email address");
                    return Page();
                }

                // Validate password length
                if (UserViewModel.Password.Length < 6)
                {
                    ModelState.AddModelError("UserViewModel.Password", "Password must be at least 6 characters long");
                    return Page();
                }

                // Validate password confirmation
                if (UserViewModel.Password != UserViewModel.ConfirmPassword)
                {
                    ModelState.AddModelError("UserViewModel.ConfirmPassword", "Passwords do not match");
                    return Page();
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(UserViewModel.Email.Trim());
                if (existingUser != null)
                {
                    _logger.LogWarning("User with email {Email} already exists", UserViewModel.Email);
                    ModelState.AddModelError("UserViewModel.Email", "A user with this email address already exists.");
                    return Page();
                }

                // Begin transaction for atomic operation
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create new user with all required fields
                    var user = new AppUser
                    {
                        UserName = UserViewModel.Email.Trim(),
                        Email = UserViewModel.Email.Trim(),
                        Name = UserViewModel.Name.Trim(),
                        Surname = UserViewModel.Surname.Trim(),
                        PhoneNumber = UserViewModel.PhoneNumber?.Trim() ?? string.Empty,
                        RegisteredDate = DateTime.Now,
                        Role = string.IsNullOrWhiteSpace(UserViewModel.Role) ? "User" : UserViewModel.Role,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        IsActive = true,
                        ReceiveNotifications = true,
                        ReceiveMarketingEmails = false,
                        PreferredLanguage = "en",
                        TimeZone = "UTC"
                    };

                    _logger.LogInformation("Creating user with email: {Email}", user.Email);
                        
                        // Execute the INSERT query to AspNetUsers table
                        var result = await _userManager.CreateAsync(user, UserViewModel.Password);
                        
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("=== USER DATABASE INSERTION SUCCESSFUL ===");
                            _logger.LogInformation("User data inserted into AspNetUsers table:");
                            _logger.LogInformation("- User ID: {UserId}", user.Id);
                            _logger.LogInformation("- Name: {Name}", user.Name);
                            _logger.LogInformation("- Surname: {Surname}", user.Surname);
                            _logger.LogInformation("- Email: {Email}", user.Email);
                            _logger.LogInformation("- Phone: {Phone}", user.PhoneNumber);
                            _logger.LogInformation("- Role: {Role}", user.Role);
                            _logger.LogInformation("- Created At: {CreatedAt}", user.CreatedAt);
                            _logger.LogInformation("- Is Active: {IsActive}", user.IsActive);
                            _logger.LogInformation("Database INSERT completed successfully for user {UserId}", user.Id);

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
                                    _logger.LogWarning("Failed to assign role {Role} to user {UserId}. Errors: {Errors}", 
                                        UserViewModel.Role, user.Id, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                                    // Don't fail the entire operation if role assignment fails
                                }
                            }

                            // Create audit log
                            try
                            {
                                var auditLog = new AuditLog
                                {
                                    EntityName = "User",
                                    EntityId = user.Id,
                                    Action = "Create",
                                    UserId = User.Identity?.Name ?? "System",
                                    Changes = $"Created new user: {user.Name} {user.Surname} (ID: {user.Id}) with role: {user.Role}",
                                    Timestamp = DateTime.UtcNow
                                };
                                await _context.AuditLogs.AddAsync(auditLog);
                                var auditSaveResult = await _context.SaveChangesAsync();
                                _logger.LogInformation("Audit log INSERT completed. Rows affected: {RowsAffected}", auditSaveResult);
                                _logger.LogInformation("Audit log created for user {UserId}", user.Id);
                            }
                            catch (Exception auditEx)
                            {
                                _logger.LogWarning("Failed to create audit log for user {UserId}: {Error}", user.Id, auditEx.Message);
                                // Don't fail the entire operation if audit log fails
                            }

                            // Commit transaction
                            await transaction.CommitAsync();
                            _logger.LogInformation("=== USER DATABASE TRANSACTION COMMITTED SUCCESSFULLY ===");
                            _logger.LogInformation("Transaction committed successfully for user {UserId}", user.Id);

                            TempData["SuccessMessage"] = $"User '{user.Name} {user.Surname}' created successfully with ID: {user.Id}.";
                            _logger.LogInformation("Redirecting to Index page with success message");
                            return RedirectToPage("/Admin/Users/Index");
                        }
                        else
                        {
                            // Rollback transaction if user creation failed
                            await transaction.RollbackAsync();
                            _logger.LogError("=== USER DATABASE INSERTION FAILED ===");
                            _logger.LogError("Failed to create user in AspNetUsers table");
                            foreach (var error in result.Errors)
                            {
                                _logger.LogError("User creation error: {Error}", error.Description);
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                }
                catch (Exception ex)
                {
                    // Rollback transaction on any error
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during user creation transaction");
                    throw;
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
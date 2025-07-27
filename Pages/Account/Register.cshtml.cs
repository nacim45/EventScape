using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace soft20181_starter.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        [BindProperty]
        public RegisterViewModel RegisterInput { get; set; } = new RegisterViewModel();

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            RegisterInput = new RegisterViewModel();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                _logger.LogInformation("Attempting to register new user with email: {Email}", RegisterInput.Email);

                // Check if email is already in use - case insensitive check
                var normalizedEmail = RegisterInput.Email.ToUpperInvariant();
                var existingUser = await _userManager.FindByEmailAsync(RegisterInput.Email);
                
                // Also check if the username exists (since we're using email as username)
                var existingUsername = await _userManager.FindByNameAsync(RegisterInput.Email);
                
                if (existingUser != null || existingUsername != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} is already in use", RegisterInput.Email);
                    ErrorMessage = "This email is already registered.";
                    return Page();
                }

                // Create the user with Identity - use the selected role from the form
                var user = new AppUser
                {
                    UserName = RegisterInput.Email, // Using email as username for simplicity
                    Email = RegisterInput.Email,
                    Name = RegisterInput.FirstName,
                    Surname = RegisterInput.LastName,
                    PhoneNumber = RegisterInput.PhoneNumber,
                    Role = RegisterInput.Role // Use the role from the form
                };

                var result = await _userManager.CreateAsync(user, RegisterInput.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} created successfully", RegisterInput.Email);

                    // Ensure the required roles exist
                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                        _logger.LogInformation("Created 'User' role");
                    }
                    
                    if (!await _roleManager.RoleExistsAsync("Administrator"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Administrator"));
                        _logger.LogInformation("Created 'Administrator' role");
                    }

                    // Assign the role based on user selection
                    if (RegisterInput.Role == "Administrator")
                    {
                        await _userManager.AddToRoleAsync(user, "Administrator");
                        _logger.LogInformation("User {Email} assigned to Administrator role", RegisterInput.Email);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                        _logger.LogInformation("User {Email} assigned to User role", RegisterInput.Email);
                    }
                    
                    // Sign in the user immediately
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    _logger.LogInformation("User {Email} signed in after registration", RegisterInput.Email);
                    
                    // Redirect administrators to admin page
                    if (RegisterInput.Role == "Administrator")
                    {
                        return RedirectToPage("/Admin/Index");
                    }
                    
                    return RedirectToPage("/Index");
                }

                // If registration failed, add errors to ModelState
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    _logger.LogWarning("Registration error: {Error}", error.Description);
                }

                ErrorMessage = "Registration failed. Please check the form and try again.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration: {Message}", ex.Message);
                ErrorMessage = "An unexpected error occurred during registration. Please try again later.";
                return Page();
            }
        }
    }
} 
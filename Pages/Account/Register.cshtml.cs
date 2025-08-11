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
using soft20181_starter.Services;

namespace soft20181_starter.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserService _userService;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IUserService userService,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
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

                // Check if email is already in use
                var existingUser = await _userService.GetUserByEmailAsync(RegisterInput.Email);
                
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} is already in use", RegisterInput.Email);
                    ErrorMessage = "This email is already registered.";
                    return Page();
                }

                // Create the user with all required fields
                var user = new AppUser
                {
                    UserName = RegisterInput.Email,
                    Email = RegisterInput.Email,
                    Name = RegisterInput.FirstName?.Trim() ?? string.Empty,
                    Surname = RegisterInput.LastName?.Trim() ?? string.Empty,
                    PhoneNumber = RegisterInput.PhoneNumber?.Trim() ?? string.Empty,
                    Role = RegisterInput.Role ?? "User",
                    RegisteredDate = DateTime.Now,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    ReceiveNotifications = true,
                    ReceiveMarketingEmails = false,
                    PreferredLanguage = "en",
                    TimeZone = "UTC",
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                // Use the UserService for robust user creation
                var createdUser = await _userService.CreateUserAsync(user, RegisterInput.Password, user.Role);

                if (createdUser != null)
                {
                    _logger.LogInformation("User {Email} created successfully with ID: {UserId}", RegisterInput.Email, createdUser.Id);

                    // Sign in the user automatically
                    await _signInManager.SignInAsync(createdUser, isPersistent: false);

                    SuccessMessage = "Registration successful! You are now logged in.";
                    _logger.LogInformation("User {Email} signed in successfully after registration", RegisterInput.Email);

                    // Redirect to home page or dashboard
                    return RedirectToPage("/Index");
                }
                else
                {
                    _logger.LogError("Failed to create user {Email}", RegisterInput.Email);
                    ErrorMessage = "Registration failed. Please try again.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", RegisterInput.Email);
                ErrorMessage = "An error occurred during registration. Please try again.";
                return Page();
            }
        }
    }
} 
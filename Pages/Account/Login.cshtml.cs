using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;

namespace soft20181_starter.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public LoginViewModel LoginInput { get; set; } = new LoginViewModel();

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // Clear previous error messages
            ErrorMessage = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _logger.LogInformation("Login attempt for email: {Email}", LoginInput.Email);

            // Try to sign in with Identity
            var result = await _signInManager.PasswordSignInAsync(
                LoginInput.Email, 
                LoginInput.Password, 
                LoginInput.RememberMe, 
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in successfully: {Email}", LoginInput.Email);
                
                // Get the user and synchronize their role status
                var user = await _userManager.FindByEmailAsync(LoginInput.Email);
                if (user != null)
                {
                    // Ensure the roles exist in the system
                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                        _logger.LogInformation("Created 'User' role during login");
                    }
                    
                    if (!await _roleManager.RoleExistsAsync("Administrator"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Administrator"));
                        _logger.LogInformation("Created 'Administrator' role during login");
                    }
                
                    var roles = await _userManager.GetRolesAsync(user);
                    
                    // Check if user's Role property matches their actual roles
                    // If user's Role property is Administrator, make sure they have that role
                    if ((user.Role == "Administrator" || user.Role == "Admin") && !roles.Contains("Administrator"))
                    {
                        await _userManager.AddToRoleAsync(user, "Administrator");
                        _logger.LogInformation("Added Administrator role to user {Email} during login", LoginInput.Email);
                        
                        // Sign in again to refresh claims
                        await _signInManager.SignInAsync(user, LoginInput.RememberMe);
                    }
                    
                    // Check for admin access if trying to access admin area
                    if (returnUrl.Contains("/Admin"))
                    {
                        // Get updated roles after possible changes
                        roles = await _userManager.GetRolesAsync(user);
                        
                        // Check if user is in administrator role
                        var isAdmin = roles.Contains("Administrator") || roles.Contains("Admin");
                        
                        if (!isAdmin)
                        {
                            _logger.LogWarning("Non-admin user {Email} attempted to access admin area", LoginInput.Email);
                            TempData["AdminAccessDenied"] = "Only administrators can access the Admin Interface.";
                            returnUrl = Url.Content("~/");
                        }
                    }
                }
                
                return LocalRedirect(returnUrl);
            }
            
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out: {Email}", LoginInput.Email);
                ErrorMessage = "Account locked out. Please try again later.";
                return Page();
            }
            
            // If we got this far, login failed
            _logger.LogWarning("Login failed for email: {Email}", LoginInput.Email);
            ErrorMessage = "Invalid email or password";
            return Page();
        }
    }
}
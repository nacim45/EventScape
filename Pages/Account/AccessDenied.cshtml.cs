using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace soft20181_starter.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        private readonly ILogger<AccessDeniedModel> _logger;

        public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(string? returnUrl = null)
        {
            _logger.LogWarning("Access denied to {ReturnUrl}. User: {User}", returnUrl, User.Identity?.Name ?? "anonymous");
        }
    }
} 
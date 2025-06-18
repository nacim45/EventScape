using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator")]
    public class DetailsModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            EventAppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public class UserViewModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Email { get; set; }
            public DateTime RegisteredDate { get; set; }
            public List<string> UserRoles { get; set; } = new List<string>();
        }

        public new UserViewModel User { get; set; }
        public List<EventAttendance> EventAttendances { get; set; } = new List<EventAttendance>();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                _logger.LogInformation("Loading user details for ID: {UserId}", id);

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", id);
                    return NotFound();
                }

                var userRoles = await _userManager.GetRolesAsync(user);

                User = new UserViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    RegisteredDate = user.RegisteredDate,
                    UserRoles = userRoles.ToList()
                };

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
    }
} 
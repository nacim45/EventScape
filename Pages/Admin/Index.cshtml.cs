using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages.Admin
{
    [Authorize(Roles = "Administrator")]
    public class IndexModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public int TotalEvents { get; set; }
        public int TotalUsers { get; set; }
        public int RecentRegistrations { get; set; }

        public IndexModel(EventAppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task OnGetAsync()
        {
            // Get total events
            TotalEvents = await _context.Events.CountAsync();
            
            // Get total users
            TotalUsers = await _userManager.Users.CountAsync();
            
            // Since IdentityUser doesn't have a CreatedDate property by default,
            // we're using a placeholder value. In a real application, you would
            // either extend IdentityUser or use a different method to track
            // recent registrations.
            RecentRegistrations = 12;
        }
    }
} 
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
    public class IndexModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            EventAppDbContext context, 
            UserManager<AppUser> userManager,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; } = string.Empty;

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Surname { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime RegisteredDate { get; set; }
            public List<string> UserRoles { get; set; } = new List<string>();
        }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading Admin Users page. Search: {Search}, Page: {Page}",
                    SearchString, CurrentPage);

                // Get all users with their roles
                var query = _userManager.Users.AsQueryable();

                // Apply search filter if specified
                if (!string.IsNullOrEmpty(SearchString))
                {
                    query = query.Where(u =>
                        u.Email.Contains(SearchString) ||
                        u.Name.Contains(SearchString) ||
                        u.Surname.Contains(SearchString)
                    );
                    _logger.LogInformation("Filtered users by search: {Search}", SearchString);
                }

                // Calculate total pages for pagination
                var totalUsers = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(totalUsers / (double)PageSize);

                // Ensure current page is within valid range
                if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }
                else if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    CurrentPage = TotalPages;
                }

                // Get paginated results
                var users = await query
                    .OrderBy(u => u.Name)
                    .ThenBy(u => u.Surname)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Build view models with roles
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    
                    Users.Add(new UserViewModel
                    {
                        Id = user.Id ?? string.Empty,
                        Name = user.Name ?? string.Empty,
                        Surname = user.Surname ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        RegisteredDate = user.RegisteredDate,
                        UserRoles = roles?.ToList() ?? new List<string>()
                    });
                }

                _logger.LogInformation("Returning {Count} users for page {Page} of {TotalPages}",
                    Users.Count, CurrentPage, TotalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin users page");
                Users = new List<UserViewModel>();
                TotalPages = 0;
            }
        }
    }
} 
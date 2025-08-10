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

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

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
        public int TotalUserCount { get; set; }
        public int AdminCount { get; set; }
        public int StandardCount { get; set; }

        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading Admin Users page. Search: {Search}, Page: {Page}, RoleFilter: {Role}",
                    SearchString, CurrentPage, RoleFilter);

                // Get all users as a starting point
                var allUsers = await _userManager.Users
                    .OrderBy(u => u.Name)
                    .ThenBy(u => u.Surname)
                    .ToListAsync();

                // Build role map for filtering and display
                var userWithRoles = new List<(AppUser User, List<string> Roles)>();
                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userWithRoles.Add((user, roles?.ToList() ?? new List<string>()));
                }

                // Totals before filters
                TotalUserCount = userWithRoles.Count;
                AdminCount = userWithRoles.Count(ur => ur.Roles.Any(r => r.Equals("Administrator", StringComparison.OrdinalIgnoreCase)));
                StandardCount = TotalUserCount - AdminCount;

                // Apply role filter
                if (!string.IsNullOrEmpty(RoleFilter))
                {
                    if (string.Equals(RoleFilter, "Administrator", StringComparison.OrdinalIgnoreCase))
                    {
                        userWithRoles = userWithRoles
                            .Where(ur => ur.Roles.Any(r => r.Equals("Administrator", StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                    }
                    else if (string.Equals(RoleFilter, "Standard", StringComparison.OrdinalIgnoreCase))
                    {
                        userWithRoles = userWithRoles
                            .Where(ur => !ur.Roles.Any(r => r.Equals("Administrator", StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(SearchString))
                {
                    userWithRoles = userWithRoles.Where(ur =>
                        (ur.User.Email?.Contains(SearchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (ur.User.Name?.Contains(SearchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (ur.User.Surname?.Contains(SearchString, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }

                // Pagination
                var totalUsers = userWithRoles.Count;
                TotalPages = (int)Math.Ceiling(totalUsers / (double)PageSize);
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                var paged = userWithRoles
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                Users = paged.Select(ur => new UserViewModel
                {
                    Id = ur.User.Id ?? string.Empty,
                    Name = ur.User.Name ?? string.Empty,
                    Surname = ur.User.Surname ?? string.Empty,
                    Email = ur.User.Email ?? string.Empty,
                    RegisteredDate = ur.User.RegisteredDate,
                    UserRoles = ur.Roles
                }).ToList();

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

        public async Task<IActionResult> OnGetUsersJsonAsync(string? roleFilter, string? searchString)
        {
            try
            {
                var allUsers = await _userManager.Users
                    .OrderBy(u => u.Name)
                    .ThenBy(u => u.Surname)
                    .ToListAsync();

                var userWithRoles = new List<(AppUser User, List<string> Roles)>();
                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userWithRoles.Add((user, roles?.ToList() ?? new List<string>()));
                }

                if (!string.IsNullOrEmpty(roleFilter))
                {
                    if (string.Equals(roleFilter, "Administrator", StringComparison.OrdinalIgnoreCase))
                    {
                        userWithRoles = userWithRoles
                            .Where(ur => ur.Roles.Any(r => r.Equals("Administrator", StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                    }
                    else if (string.Equals(roleFilter, "Standard", StringComparison.OrdinalIgnoreCase))
                    {
                        userWithRoles = userWithRoles
                            .Where(ur => !ur.Roles.Any(r => r.Equals("Administrator", StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                    }
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    userWithRoles = userWithRoles.Where(ur =>
                        (ur.User.Email?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (ur.User.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (ur.User.Surname?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }

                var result = userWithRoles.Select(ur => new {
                    id = ur.User.Id,
                    name = ur.User.Name,
                    surname = ur.User.Surname,
                    email = ur.User.Email,
                    roles = ur.Roles,
                    registeredDate = ur.User.RegisteredDate
                }).ToList();

                return new JsonResult(new { users = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building users json");
                return new JsonResult(new { users = Array.Empty<object>() });
            }
        }
    }
} 
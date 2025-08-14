using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using soft20181_starter.Models;
using soft20181_starter.Services;

namespace soft20181_starter.Pages
{
    public class AddEventModel : PageModel
    {
        private readonly EventAppDbContext _context;
        private readonly SimpleAuditService _auditService;

        public AddEventModel(EventAppDbContext context, SimpleAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [BindProperty]
        public TheEvent theEvent { get; set; } = new TheEvent();

        public void OnGet()
        {
            // Initialize any default values if needed
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Events.Add(theEvent);
            await _context.SaveChangesAsync();

            // Audit log the event creation
            await _auditService.LogCreateAsync(theEvent);

            // Redirect to the events list page after successful save
            return RedirectToPage("Events");
        }
    }
}


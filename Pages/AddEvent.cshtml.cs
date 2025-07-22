using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using soft20181_starter.Models;

namespace soft20181_starter.Pages
{
    public class AddEventModel : PageModel
    {
        private readonly EventAppDbContext _context;

        public AddEventModel(EventAppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TheEvent theEvent { get; set; } = new TheEvent();

        public void OnGet()
        {
            // Initialize any default values if needed
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Events.Add(theEvent);
            _context.SaveChanges();

            // Redirect to the events list page after successful save
            return RedirectToPage("Events");
        }
    }
}


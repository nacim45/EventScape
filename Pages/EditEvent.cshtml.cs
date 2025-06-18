using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Models;

namespace soft20181_starter.Pages
{
    public class EditEventModel : PageModel
    {
        private readonly EventAppDbContext dbContext;

        public EditEventModel(EventAppDbContext _db)
        {
            dbContext = _db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public TheEvent theEvent { get; set; } = new TheEvent();

        public void OnGet()
        {
            var foundEvent = dbContext.Events.Find(Id);
            if (foundEvent == null)
            {
                RedirectToPage("Events");
                return;
            }
            
            theEvent = foundEvent;
        }
        

        public IActionResult OnPost()
        {
            if (ModelState.IsValid)
            {
                dbContext.Events.Update(theEvent);
                dbContext.SaveChanges();
                return RedirectToPage("Events");
            }
            else
            {
                return Page();
            }
        }
        
        public IActionResult OnGetDelete()
        {
            var eventToUpdate = dbContext.Events.Find(Id);
            if (eventToUpdate != null)
            {
                eventToUpdate.IsDeleted = true;
                dbContext.Events.Update(eventToUpdate);
                dbContext.SaveChanges();
            }
            return RedirectToPage("Events");
        }
    }
}


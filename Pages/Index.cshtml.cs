using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Pages
{
    public class IndexModel : PageModel
    {
        private readonly EventAppDbContext _dbContext;

        public IndexModel(EventAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Properties for different event categories
        public List<TheEvent> NottinghamEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> LondonEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> FeaturedEvents { get; set; } = new List<TheEvent>();

        // Added properties for additional locations
        public List<TheEvent> ManchesterEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> BirminghamEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> LiverpoolEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> EdinburghEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> GlasgowEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> DublinEvents { get; set; } = new List<TheEvent>();

        // Dictionary to hold events by location for easy access
        public Dictionary<string, List<TheEvent>> EventsByLocation { get; set; } = new Dictionary<string, List<TheEvent>>();

        // Properties for trending or upcoming events
        public List<TheEvent> UpcomingEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> TrendingEvents { get; set; } = new List<TheEvent>();

        // Properties for free events and recently added events
        public List<TheEvent> FreeEvents { get; set; } = new List<TheEvent>();
        public List<TheEvent> RecentlyAddedEvents { get; set; } = new List<TheEvent>();

        public async Task OnGetAsync()
        {
            // Get all events
            var allActiveEvents = await _dbContext.Events
                .ToListAsync();

            // Process events by location
            InitializeEventsByLocation(allActiveEvents);

            // Set specific location events
            NottinghamEvents = EventsByLocation.GetValueOrDefault("nottingham", new List<TheEvent>());
            LondonEvents = EventsByLocation.GetValueOrDefault("london", new List<TheEvent>());
            ManchesterEvents = EventsByLocation.GetValueOrDefault("manchester", new List<TheEvent>());
            BirminghamEvents = EventsByLocation.GetValueOrDefault("birmingham", new List<TheEvent>());
            LiverpoolEvents = EventsByLocation.GetValueOrDefault("liverpool", new List<TheEvent>());
            EdinburghEvents = EventsByLocation.GetValueOrDefault("edinburgh", new List<TheEvent>());
            GlasgowEvents = EventsByLocation.GetValueOrDefault("glasgow", new List<TheEvent>());
            DublinEvents = EventsByLocation.GetValueOrDefault("dublin", new List<TheEvent>());

            // Generate featured events - select some events based on criteria or random selection
            FeaturedEvents = GetFeaturedEvents(allActiveEvents);

            // Get upcoming events - ordered by date
            UpcomingEvents = GetUpcomingEvents(allActiveEvents);

            // Get trending events - this could be based on popularity or other criteria
            TrendingEvents = GetTrendingEvents(allActiveEvents);

            // Get free events - filter events with price equal to "Free" or "0"
            FreeEvents = GetFreeEvents(allActiveEvents);

            // Get recently added events - this would require a timestamp field in your model
            // For now, we'll just take the latest events based on ID
            RecentlyAddedEvents = GetRecentlyAddedEvents(allActiveEvents);
        }

        // Helper method to organize events by location
        private void InitializeEventsByLocation(List<TheEvent> events)
        {
            // Group events by location (case insensitive)
            var groupedEvents = events
                .GroupBy(e => e.location.ToLower())
                .ToDictionary(g => g.Key, g => g.ToList());

            // Initialize the dictionary with all relevant locations
            string[] locations = { "nottingham", "london", "manchester", "birmingham",
                                  "liverpool", "edinburgh", "glasgow", "dublin" };

            foreach (var location in locations)
            {
                EventsByLocation[location] = groupedEvents.GetValueOrDefault(location, new List<TheEvent>());
            }
        }

        // Helper method to get featured events
        private List<TheEvent> GetFeaturedEvents(List<TheEvent> allEvents)
        {
            // In a real application, you might have a "Featured" flag in your model
            // For now, we'll pick 4 events at random
            return allEvents
                .OrderBy(e => Guid.NewGuid()) // Random order
                .Take(4)
                .ToList();
        }

        // Helper method to get upcoming events
        private List<TheEvent> GetUpcomingEvents(List<TheEvent> allEvents)
        {
            // Parse dates and order by ascending date
            // Note: This assumes date is stored in a format that can be parsed
            try
            {
                return allEvents
                    .OrderBy(e => DateTime.Parse(e.date))
                    .Take(5)
                    .ToList();
            }
            catch
            {
                // Fallback if date parsing fails
                return allEvents.Take(5).ToList();
            }
        }

        // Helper method to get trending events
        private List<TheEvent> GetTrendingEvents(List<TheEvent> allEvents)
        {
            // In a real app, this might be based on view counts or registrations
            // For now, we'll just select some events
            return allEvents
                .OrderBy(e => Guid.NewGuid())
                .Take(3)
                .ToList();
        }

        // Helper method to get free events
        private List<TheEvent> GetFreeEvents(List<TheEvent> allEvents)
        {
            // Filter events with price equal to "Free" or "0"
            return allEvents
                .Where(e => e.price.ToLower() == "free" || e.price == "0" || e.price == "�0")
                .Take(4)
                .ToList();
        }

        // Helper method to get recently added events
        private List<TheEvent> GetRecentlyAddedEvents(List<TheEvent> allEvents)
        {
            // In a real app, this would use a timestamp field
            // For now, we'll use IDs as a proxy for recency
            return allEvents
                .OrderByDescending(e => e.id)
                .Take(3)
                .ToList();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using soft20181_starter.Models;

namespace soft20181_starter.Data
{   
    public class DatabaseSeeder
    {
        // Global ID counter to ensure uniqueness
        private static int _globalEventId = 1;
        
        public static async Task SeedEvents(EventAppDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            // Check if database already has events
            if (await dbContext.Events.AnyAsync())
            {
                Console.WriteLine("Database already contains events. Skipping seeding.");
                return;
            }

            // Define locations and their corresponding JSON files
            var locationFiles = new Dictionary<string, string>
            {
                { "nottingham", "data/events.json" },
                { "london", "data/events2.json" },
                { "manchester", "data/events3.json" },
                { "liverpool", "data/events4.json" },
                { "birmingham", "data/events5.json" },
                { "dublin", "data/events6.json" },
                { "glasgow", "data/events7.json" },
                { "edinburgh", "data/events8.json" },
            };

            int totalEventsAdded = 0;
            var failedLocations = new List<string>();
            _globalEventId = 1; // Reset the ID counter

            // Process each location file
            foreach (var locationFile in locationFiles)
            {
                var location = locationFile.Key;
                var relativeFilePath = locationFile.Value;
                var filePath = Path.Combine(webHostEnvironment.WebRootPath, relativeFilePath);

                // Check if file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Warning: File not found for {location} at path {filePath}");
                    failedLocations.Add(location);
                    continue;
                }

                try
                {
                    Console.WriteLine($"Processing events for {location}...");
                    var locationEventsAdded = await ProcessLocationFile(dbContext, filePath, location);
                    totalEventsAdded += locationEventsAdded;
                    Console.WriteLine($"Added {locationEventsAdded} events from {location}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {location} events: {ex.Message}");
                    failedLocations.Add(location);
                }
            }

            // Save all changes to the database after processing all files
            try
            {
                var changes = await dbContext.SaveChangesAsync();
                Console.WriteLine($"Database seeding completed. Added {totalEventsAdded} events total.");

                if (failedLocations.Any())
                {
                    Console.WriteLine($"Warning: Failed to process these locations: {string.Join(", ", failedLocations)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes to database: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static async Task<int> ProcessLocationFile(EventAppDbContext dbContext, string filePath, string location)
        {
            string jsonContent;
            try 
            {
                jsonContent = await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                return 0;
            }

            List<JsonEvent> jsonEvents;
            try 
            {
                jsonEvents = JsonConvert.DeserializeObject<List<JsonEvent>>(jsonContent) ?? new List<JsonEvent>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing JSON from {filePath}: {ex.Message}");
                return 0;
            }

            if (jsonEvents == null || !jsonEvents.Any())
            {
                Console.WriteLine($"No events found in file: {filePath}");
                return 0;
            }

            int validEvents = 0;

            foreach (var jsonEvent in jsonEvents)
            {
                try
                {
                    // Force the correct location based on the file being processed
                    jsonEvent.location = location;

                    if (!ValidateJsonEvent(jsonEvent))
                    {
                        Console.WriteLine($"Skipping invalid event in {location}: {jsonEvent.title ?? "Untitled"}");
                        continue;
                    }

                    // Use the global ID counter to ensure uniqueness
                    var dbEvent = CreateDatabaseEvent(jsonEvent, location, _globalEventId++);
                    dbContext.Events.Add(dbEvent);
                    validEvents++;

                    // Save in batches to avoid memory issues
                    if (validEvents % 20 == 0)
                    {
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing event in {location}: {ex.Message}");
                }
            }

            // Save any remaining events
            if (validEvents % 20 != 0)
            {
                await dbContext.SaveChangesAsync();
            }

            return validEvents;
        }

        private static bool ValidateJsonEvent(JsonEvent jsonEvent)
        {
            if (string.IsNullOrWhiteSpace(jsonEvent.title))
            {
                Console.WriteLine("Event missing title");
                return false;
            }

            if (string.IsNullOrWhiteSpace(jsonEvent.description))
            {
                Console.WriteLine($"Event '{jsonEvent.title}' missing description");
                return false;
            }

            // Initialize the images list if null
            if (jsonEvent.images == null)
            {
                jsonEvent.images = new List<string>();
            }

            // If we have a single image property but no images array, add it to the array
            if (!string.IsNullOrWhiteSpace(jsonEvent.image) && !jsonEvent.images.Contains(jsonEvent.image))
            {
                jsonEvent.images.Add(jsonEvent.image);
            }

            return true;
        }

        private static TheEvent CreateDatabaseEvent(JsonEvent jsonEvent, string location, int eventId)
        {
            // Clean and format data
            var cleanTitle = jsonEvent.title?.Trim() ?? "Untitled Event";
            var cleanDescription = jsonEvent.description?.Trim() ?? "No description available";
            var cleanImages = jsonEvent.images?
                .Where(img => !string.IsNullOrWhiteSpace(img))
                .Select(img => img.Trim())
                .ToList() ?? new List<string>();

            // Fix URLs in link property - ensure proper URL format
            string eventLink = jsonEvent.link;
            if (!string.IsNullOrEmpty(eventLink))
            {
                // Fix any URLs with multiple question marks
                if (eventLink.Contains("?") && eventLink.LastIndexOf("?") != eventLink.IndexOf("?"))
                {
                    eventLink = eventLink.Replace("?id=", "&id=")
                                       .Replace("?location=", "&location=");
                    
                    // Make sure there's only one initial question mark
                    int firstQuestionMark = eventLink.IndexOf("?");
                    if (firstQuestionMark >= 0)
                    {
                        string baseUrl = eventLink.Substring(0, firstQuestionMark + 1);
                        string queryPart = eventLink.Substring(firstQuestionMark + 1)
                                                   .Replace("?", "&");
                        eventLink = baseUrl + queryPart;
                    }
                }
                
                // For EventDetail links, ensure they go to the right page with correct parameters
                if (eventLink.Contains("/EventDetail"))
                {
                    eventLink = $"/EventDetail?location={location}&id={eventId}";
                }
            }

            return new TheEvent
            {
                id = eventId,
                title = cleanTitle.Length > 100 ? cleanTitle.Substring(0, 100) : cleanTitle,
                images = cleanImages,
                description = cleanDescription,
                date = FormatEventDate(jsonEvent.date),
                price = FormatEventPrice(jsonEvent.price),
                link = eventLink,
                location = location.ToLower()
            };
        }

        private static string FormatEventDate(string inputDate)
        {
            if (string.IsNullOrWhiteSpace(inputDate))
                return "Date to be confirmed";

            if (DateTime.TryParse(inputDate, out var date))
            {
                return date.ToString("dddd, dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }

            // Return as-is if parsing fails
            return inputDate;
        }

        private static string FormatEventPrice(string inputPrice)
        {
            if (string.IsNullOrWhiteSpace(inputPrice))
                return "Price to be confirmed";

            // Normalize "free" text
            if (inputPrice.ToLower() == "free" || inputPrice == "0" || inputPrice == "£0")
                return "Free";

            // Ensure consistent £ symbol
            if (inputPrice.Contains("£"))
                return inputPrice;

            if (decimal.TryParse(inputPrice, out var price))
                return $"£{price}";

            return inputPrice;
        }

        // Class representing JSON structure
        public class JsonEvent
        {
            public string id { get; set; } = string.Empty;
            public string title { get; set; } = string.Empty;
            public List<string> images { get; set; } = new List<string>();
            public string image { get; set; } = string.Empty;
            public string description { get; set; } = string.Empty;
            public string date { get; set; } = string.Empty;
            public string price { get; set; } = string.Empty;
            public string link { get; set; } = string.Empty;
            public string location { get; set; } = string.Empty;
        }
    }
}




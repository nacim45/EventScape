using Microsoft.Extensions.Logging;
using soft20181_starter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace soft20181_starter.Services
{
    public class EventService : IEventService
    {
        private readonly EventAppDbContext _context;
        private readonly ILogger<EventService> _logger;

        public EventService(EventAppDbContext context, ILogger<EventService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Event> GetEvents()
        {
            try
            {
                // Convert from database model to application model
                return _context.Events
                    .Where(e => !e.IsDeleted)
                    .Select(e => new Event
                    {
                        Id = e.id,
                        Title = e.title,
                        Location = e.location,
                        Date = DateTime.Parse(e.date),
                        Price = decimal.Parse(e.price),
                        Description = e.description,
                        Images = e.images != null ? e.images.ToList() : new List<string>(),
                        Link = e.link,
                        IsDeleted = e.IsDeleted
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return new List<Event>();
            }
        }

        public Event GetEventById(int id)
        {
            try
            {
                var dbEvent = _context.Events.FirstOrDefault(e => e.id == id && !e.IsDeleted);
                if (dbEvent == null)
                {
                    return null;
                }

                return new Event
                {
                    Id = dbEvent.id,
                    Title = dbEvent.title,
                    Location = dbEvent.location,
                    Date = DateTime.Parse(dbEvent.date),
                    Price = decimal.Parse(dbEvent.price),
                    Description = dbEvent.description,
                    Images = dbEvent.images != null ? dbEvent.images.ToList() : new List<string>(),
                    Link = dbEvent.link,
                    IsDeleted = dbEvent.IsDeleted
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event with ID {EventId}", id);
                return null;
            }
        }

        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            try
            {
                var dbEvent = new TheEvent
                {
                    title = newEvent.Title,
                    location = newEvent.Location,
                    date = newEvent.Date.ToString("yyyy-MM-dd"),
                    price = newEvent.Price.ToString(),
                    description = newEvent.Description,
                    images = newEvent.Images,
                    link = newEvent.Link,
                    IsDeleted = false
                };

                _context.Events.Add(dbEvent);
                await _context.SaveChangesAsync();

                newEvent.Id = dbEvent.id;
                return newEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new event");
                return null;
            }
        }

        public async Task<Event> UpdateEventAsync(Event updatedEvent)
        {
            try
            {
                var dbEvent = await _context.Events.FindAsync(updatedEvent.Id);
                if (dbEvent == null || dbEvent.IsDeleted)
                {
                    return null;
                }

                dbEvent.title = updatedEvent.Title;
                dbEvent.location = updatedEvent.Location;
                dbEvent.date = updatedEvent.Date.ToString("yyyy-MM-dd");
                dbEvent.price = updatedEvent.Price.ToString();
                dbEvent.description = updatedEvent.Description;
                dbEvent.images = updatedEvent.Images;
                dbEvent.link = updatedEvent.Link;

                await _context.SaveChangesAsync();
                return updatedEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event with ID {EventId}", updatedEvent.Id);
                return null;
            }
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            try
            {
                var dbEvent = await _context.Events.FindAsync(id);
                if (dbEvent == null)
                {
                    return false;
                }

                // Soft delete - mark as deleted instead of removing
                dbEvent.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event with ID {EventId}", id);
                return false;
            }
        }
    }
} 
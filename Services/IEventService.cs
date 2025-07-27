using soft20181_starter.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace soft20181_starter.Services
{
    public interface IEventService
    {
        List<Event> GetEvents();
        Event GetEventById(int id);
        Task<Event> CreateEventAsync(Event newEvent);
        Task<Event> UpdateEventAsync(Event updatedEvent);
        Task<bool> DeleteEventAsync(int id);
    }
} 
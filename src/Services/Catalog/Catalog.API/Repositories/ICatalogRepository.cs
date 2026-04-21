using Catalog.API.Entities;
using MongoDB.Driver;

namespace Catalog.API.Repositories;

public interface ICatalogRepository
{
    Task<IEnumerable<Event>> GetEvents();
    Task<Event?> GetEvent(string id);
    Task<IEnumerable<Event>> GetEventsByCategory(string category);
    Task<IEnumerable<Event>> GetEventsByName(string name);
    Task CreateEvent(Event @event);
    Task<bool> UpdateEvent(Event @event);
    Task<bool> DeleteEvent(string id);
}

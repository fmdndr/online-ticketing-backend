using Catalog.API.Data;
using Catalog.API.Entities;
using MongoDB.Driver;

namespace Catalog.API.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly ICatalogContext _context;

    public CatalogRepository(ICatalogContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetEvents()
    {
        return await _context.Events.Find(e => true).ToListAsync();
    }

    public async Task<Event?> GetEvent(string id)
    {
        return await _context.Events.Find(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByCategory(string category)
    {
        return await _context.Events
            .Find(e => e.Category.ToLower() == category.ToLower())
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByName(string name)
    {
        var filter = Builders<Event>.Filter
            .Regex(e => e.Name, new MongoDB.Bson.BsonRegularExpression(name, "i"));
        return await _context.Events.Find(filter).ToListAsync();
    }

    public async Task CreateEvent(Event @event)
    {
        await _context.Events.InsertOneAsync(@event);
    }

    public async Task<bool> UpdateEvent(Event @event)
    {
        var result = await _context.Events
            .ReplaceOneAsync(e => e.Id == @event.Id, @event);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteEvent(string id)
    {
        var result = await _context.Events
            .DeleteOneAsync(e => e.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}

using Catalog.API.Entities;
using MongoDB.Driver;

namespace Catalog.API.Data;

public static class CatalogContextSeed
{
    public static void SeedData(IMongoCollection<Event> eventCollection)
    {
        bool existEvents = eventCollection.Find(e => true).Any();
        if (existEvents) return;

        var events = new List<Event>
        {
            new()
            {
                Name = "Rock Festival 2026",
                Description = "The biggest rock festival of the year featuring top international bands and artists. Three stages, food courts, and camping facilities available.",
                Category = "Music",
                ImageUrl = "https://picsum.photos/seed/rock/800/400",
                Venue = "Central Park Arena, Istanbul",
                Date = new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc),
                TicketTypes = new List<TicketType>
                {
                    new() { Name = "General Admission", Price = 150.00m, AvailableQuantity = 500 },
                    new() { Name = "VIP", Price = 350.00m, AvailableQuantity = 100 },
                    new() { Name = "Backstage Pass", Price = 750.00m, AvailableQuantity = 20 }
                }
            },
            new()
            {
                Name = "Tech Conference 2026",
                Description = "Annual technology conference covering AI, cloud computing, microservices architecture, and emerging tech trends. Networking opportunities included.",
                Category = "Technology",
                ImageUrl = "https://picsum.photos/seed/tech/800/400",
                Venue = "Convention Center, Ankara",
                Date = new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Utc),
                TicketTypes = new List<TicketType>
                {
                    new() { Name = "Standard", Price = 200.00m, AvailableQuantity = 300 },
                    new() { Name = "Premium", Price = 450.00m, AvailableQuantity = 50 },
                }
            },
            new()
            {
                Name = "Comedy Night Live",
                Description = "An evening of stand-up comedy featuring the funniest comedians in the country. Dinner and drinks packages available.",
                Category = "Entertainment",
                ImageUrl = "https://picsum.photos/seed/comedy/800/400",
                Venue = "Grand Theatre, Izmir",
                Date = new DateTime(2026, 6, 10, 20, 0, 0, DateTimeKind.Utc),
                TicketTypes = new List<TicketType>
                {
                    new() { Name = "Regular", Price = 80.00m, AvailableQuantity = 200 },
                    new() { Name = "Front Row", Price = 180.00m, AvailableQuantity = 30 },
                    new() { Name = "Dinner Package", Price = 280.00m, AvailableQuantity = 50 }
                }
            }
        };

        eventCollection.InsertMany(events);
    }
}

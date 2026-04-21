namespace Shared.Common.Models;

public class Event
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<TicketType> TicketTypes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

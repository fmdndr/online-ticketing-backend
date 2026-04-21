using MongoDB.Bson.Serialization.Attributes;

namespace Catalog.API.Entities;

public class TicketType
{
    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("availableQuantity")]
    public int AvailableQuantity { get; set; }
}

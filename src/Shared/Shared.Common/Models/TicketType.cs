namespace Shared.Common.Models;

public class TicketType
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
}

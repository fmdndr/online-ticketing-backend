namespace Shared.Common.Models;

public class BasketItem
{
    public string EventId { get; set; } = null!;
    public string EventName { get; set; } = null!;
    public string TicketTypeName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

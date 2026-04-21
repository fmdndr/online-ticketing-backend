namespace Shared.Common.Events;

public record TicketReservedEvent
{
    public string OrderId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string EventId { get; init; } = null!;
    public string TicketTypeName { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTime ReservedAt { get; init; } = DateTime.UtcNow;
}

namespace Shared.Common.Events;

public record PaymentCompletedEvent
{
    public string OrderId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public decimal TotalPrice { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

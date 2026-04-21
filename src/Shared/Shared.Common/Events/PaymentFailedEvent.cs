namespace Shared.Common.Events;

public record PaymentFailedEvent
{
    public string OrderId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
}

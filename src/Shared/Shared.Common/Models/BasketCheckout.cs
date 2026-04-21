namespace Shared.Common.Models;

public class BasketCheckout
{
    public string UserId { get; set; } = null!;
    public decimal TotalPrice { get; set; }
    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }
}

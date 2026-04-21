namespace Shared.Common.Models;

public class ShoppingCart
{
    public string UserId { get; set; } = null!;
    public List<BasketItem> Items { get; set; } = new();
    
    public decimal TotalPrice => Items.Sum(i => i.UnitPrice * i.Quantity);
}

namespace Shared.Common.Exceptions;

public class PaymentFailedException : Exception
{
    public PaymentFailedException(string orderId, string reason)
        : base($"Payment for order '{orderId}' failed: {reason}") { }
}

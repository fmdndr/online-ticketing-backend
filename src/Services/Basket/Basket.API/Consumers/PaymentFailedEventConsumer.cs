using Basket.API.Repositories;
using MassTransit;
using Shared.Common.Events;

namespace Basket.API.Consumers;

/// <summary>
/// Handles failed payment — releases the distributed lock (compensating transaction).
/// </summary>
public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly IBasketRepository _repository;
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(IBasketRepository repository, ILogger<PaymentFailedEventConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning("Payment FAILED for order {OrderId}, user {UserId}. Reason: {Reason}. Executing compensating transaction.",
            message.OrderId, message.UserId, message.Reason);

        // Retrieve the basket to know which locks to release
        var basket = await _repository.GetBasket(message.UserId);
        if (basket != null)
        {
            foreach (var item in basket.Items)
            {
                await _repository.ReleaseLock(item.EventId, item.TicketTypeName, message.UserId);
                _logger.LogInformation("Lock released for event {EventId}, ticket {TicketType} (compensating transaction)",
                    item.EventId, item.TicketTypeName);
            }
        }

        _logger.LogInformation("Compensating transaction completed for user {UserId}", message.UserId);
    }
}

using Basket.API.Repositories;
using MassTransit;
using Shared.Common.Events;

namespace Basket.API.Consumers;

/// <summary>
/// Handles successful payment — clears the user's basket.
/// </summary>
public class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IBasketRepository _repository;
    private readonly ILogger<PaymentCompletedEventConsumer> _logger;

    public PaymentCompletedEventConsumer(IBasketRepository repository, ILogger<PaymentCompletedEventConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Payment completed for order {OrderId}, user {UserId}. Clearing basket.",
            message.OrderId, message.UserId);

        await _repository.DeleteBasket(message.UserId);

        _logger.LogInformation("Basket cleared for user {UserId} after successful payment", message.UserId);
    }
}

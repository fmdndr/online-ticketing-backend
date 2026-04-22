using MassTransit;
using Payment.API.Data;
using Payment.API.Entities;
using Shared.Common.Events;

namespace Payment.API.Consumers;

/// <summary>
/// Consumes TicketReservedEvent, simulates payment processing,
/// and publishes either PaymentCompletedEvent or PaymentFailedEvent.
/// </summary>
public class TicketReservedEventConsumer : IConsumer<TicketReservedEvent>
{
    private readonly PaymentDbContext _dbContext;
    private readonly ITopicProducer<PaymentCompletedEvent> _completedProducer;
    private readonly ITopicProducer<PaymentFailedEvent> _failedProducer;
    private readonly ILogger<TicketReservedEventConsumer> _logger;
    private static readonly Random _random = new();

    public TicketReservedEventConsumer(
        PaymentDbContext dbContext,
        ITopicProducer<PaymentCompletedEvent> completedProducer,
        ITopicProducer<PaymentFailedEvent> failedProducer,
        ILogger<TicketReservedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _completedProducer = completedProducer;
        _failedProducer = failedProducer;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketReservedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Processing payment for order {OrderId}, user {UserId}, amount {Amount}",
            message.OrderId, message.UserId, message.TotalPrice);

        var paymentRecord = new PaymentRecord
        {
            OrderId = message.OrderId,
            UserId = message.UserId,
            EventId = message.EventId,
            TicketTypeName = message.TicketTypeName,
            Quantity = message.Quantity,
            TotalAmount = message.TotalPrice,
            Status = PaymentStatus.Pending
        };

        // Simulate payment processing delay
        await Task.Delay(TimeSpan.FromSeconds(2));

        // 80% success rate simulation
        var isSuccess = _random.Next(100) < 80;

        if (isSuccess)
        {
            paymentRecord.Status = PaymentStatus.Completed;
            paymentRecord.CompletedAt = DateTime.UtcNow;

            _dbContext.Payments.Add(paymentRecord);
            await _dbContext.SaveChangesAsync();

            await _completedProducer.Produce(new PaymentCompletedEvent
            {
                OrderId = message.OrderId,
                UserId = message.UserId,
                TotalPrice = message.TotalPrice,
                CompletedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Payment COMPLETED for order {OrderId}", message.OrderId);
        }
        else
        {
            var reason = "Simulated payment failure — insufficient funds.";
            paymentRecord.Status = PaymentStatus.Failed;
            paymentRecord.FailureReason = reason;
            paymentRecord.CompletedAt = DateTime.UtcNow;

            _dbContext.Payments.Add(paymentRecord);
            await _dbContext.SaveChangesAsync();

            await _failedProducer.Produce(new PaymentFailedEvent
            {
                OrderId = message.OrderId,
                UserId = message.UserId,
                Reason = reason,
                FailedAt = DateTime.UtcNow
            });

            _logger.LogWarning("Payment FAILED for order {OrderId}: {Reason}", message.OrderId, reason);
        }
    }
}

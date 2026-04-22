using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;
using Payment.API.Entities;
using Payment.API.Kafka;
using Shared.Common.Events;

namespace Payment.API.Consumers;

/// <summary>
/// Background consumer: listens on topic "ticket-reserved", simulates payment,
/// writes PaymentRecord, then produces to payment-completed or payment-failed.
/// </summary>
public class TicketReservedEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaProducer _producer;
    private readonly ILogger<TicketReservedEventConsumer> _logger;
    private static readonly Random _random = new();

    public TicketReservedEventConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        IKafkaProducer producer,
        ILogger<TicketReservedEventConsumer> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "payment-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe("ticket-reserved");
        _logger.LogInformation("TicketReservedEventConsumer started, subscribed to ticket-reserved");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result == null) continue;

                    var message = JsonSerializer.Deserialize<TicketReservedEvent>(result.Message.Value);
                    if (message == null) { consumer.Commit(result); continue; }

                    _logger.LogInformation("Processing payment for order {OrderId}, user {UserId}, amount {Amount}",
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
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                    // 80% success rate simulation
                    var isSuccess = _random.Next(100) < 80;

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                    if (isSuccess)
                    {
                        paymentRecord.Status = PaymentStatus.Completed;
                        paymentRecord.CompletedAt = DateTime.UtcNow;
                        dbContext.Payments.Add(paymentRecord);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        await _producer.ProduceAsync("payment-completed", new PaymentCompletedEvent
                        {
                            OrderId = message.OrderId,
                            UserId = message.UserId,
                            TotalPrice = message.TotalPrice,
                            CompletedAt = DateTime.UtcNow
                        }, stoppingToken);

                        _logger.LogInformation("Payment COMPLETED for order {OrderId}", message.OrderId);
                    }
                    else
                    {
                        var reason = "Simulated payment failure — insufficient funds.";
                        paymentRecord.Status = PaymentStatus.Failed;
                        paymentRecord.FailureReason = reason;
                        paymentRecord.CompletedAt = DateTime.UtcNow;
                        dbContext.Payments.Add(paymentRecord);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        await _producer.ProduceAsync("payment-failed", new PaymentFailedEvent
                        {
                            OrderId = message.OrderId,
                            UserId = message.UserId,
                            Reason = reason,
                            FailedAt = DateTime.UtcNow
                        }, stoppingToken);

                        _logger.LogWarning("Payment FAILED for order {OrderId}: {Reason}", message.OrderId, reason);
                    }

                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error on ticket-reserved: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error processing TicketReservedEvent");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("TicketReservedEventConsumer stopped");
        }
    }
}

using System.Text.Json;
using Basket.API.Repositories;
using Confluent.Kafka;
using Shared.Common.Events;

namespace Basket.API.Consumers;

public class PaymentFailedEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentFailedEventConsumer> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "basket-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe("payment-failed");
        _logger.LogInformation("PaymentFailedEventConsumer started, subscribed to payment-failed");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result == null) continue;

                    var @event = JsonSerializer.Deserialize<PaymentFailedEvent>(result.Message.Value);
                    if (@event == null) { consumer.Commit(result); continue; }

                    _logger.LogWarning("Payment FAILED for order {OrderId}, user {UserId}. Reason: {Reason}. Executing compensating transaction.",
                        @event.OrderId, @event.UserId, @event.Reason);

                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IBasketRepository>();

                    var basket = await repository.GetBasket(@event.UserId);
                    if (basket != null)
                    {
                        foreach (var item in basket.Items)
                        {
                            await repository.ReleaseLock(item.EventId, item.TicketTypeName, @event.UserId);
                            _logger.LogInformation("Lock released for event {EventId}, ticket {TicketType} (compensating transaction)",
                                item.EventId, item.TicketTypeName);
                        }
                    }

                    _logger.LogInformation("Compensating transaction completed for user {UserId}", @event.UserId);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error on payment-failed: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error processing PaymentFailedEvent");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("PaymentFailedEventConsumer stopped");
        }
    }
}

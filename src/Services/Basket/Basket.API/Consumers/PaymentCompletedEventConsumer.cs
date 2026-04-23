using System.Text.Json;
using Basket.API.Repositories;
using Confluent.Kafka;
using Shared.Common.Events;

namespace Basket.API.Consumers;

public class PaymentCompletedEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentCompletedEventConsumer> _logger;

    public PaymentCompletedEventConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentCompletedEventConsumer> logger)
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
        consumer.Subscribe("payment-completed");
        _logger.LogInformation("PaymentCompletedEventConsumer started, subscribed to payment-completed");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result == null) continue;

                    var @event = JsonSerializer.Deserialize<PaymentCompletedEvent>(result.Message.Value);
                    if (@event == null) { consumer.Commit(result); continue; }

                    _logger.LogInformation("Payment completed for order {OrderId}, user {UserId}. Clearing basket.",
                        @event.OrderId, @event.UserId);

                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IBasketRepository>();
                    await repository.DeleteBasket(@event.UserId);

                    _logger.LogInformation("Basket cleared for user {UserId} after successful payment", @event.UserId);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error on payment-completed: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error processing PaymentCompletedEvent");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("PaymentCompletedEventConsumer stopped");
        }
    }
}

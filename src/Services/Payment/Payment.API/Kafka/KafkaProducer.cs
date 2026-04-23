using System.Text.Json;
using Confluent.Kafka;

namespace Payment.API.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
}

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = json
        };

        var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
        _logger.LogDebug("Produced message to {Topic} [{Partition}@{Offset}]",
            topic, result.Partition.Value, result.Offset.Value);
    }

    public void Dispose() => _producer.Dispose();
}

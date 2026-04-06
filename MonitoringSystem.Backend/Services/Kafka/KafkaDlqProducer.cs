using Confluent.Kafka;
using MonitoringSystem.Shared.Models;
using System.Text.Json;

namespace MonitoringSystem.Backend.Services.Kafka;

public class KafkaDlqProducer : IAsyncDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _dlqTopic;
    private readonly ILogger<KafkaDlqProducer> _logger;

    public KafkaDlqProducer(IConfiguration configuration, ILogger<KafkaDlqProducer> logger)
    {
        _logger = logger;
        _dlqTopic = configuration["Kafka:DlqTopic"] ?? "sensor-topic-dlq";

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    public async Task SendAsync(string originalTopic, string originalPayload, Exception ex, CancellationToken cancellationToken = default)
    {
        var dlqMessage = new DlqMessage
        {
            OriginalTopic = originalTopic,
            OriginalPayload = originalPayload,
            ErrorType = ex.GetType().Name,
            ErrorMessage = ex.Message,
            FailedAt = DateTime.UtcNow
        };

        var payload = JsonSerializer.Serialize(dlqMessage);

        try
        {
            var result = await _producer.ProduceAsync(
                _dlqTopic,
                new Message<Null, string> { Value = payload },
                cancellationToken);

            _logger.LogWarning(
                "메시지를 DLQ로 이동. DlqTopic={DlqTopic}, Offset={Offset}, ErrorType={ErrorType}, Error={Error}",
                _dlqTopic,
                result.TopicPartitionOffset,
                dlqMessage.ErrorType,
                dlqMessage.ErrorMessage);
        }
        catch (Exception dlqEx)
        {
            _logger.LogError(dlqEx, "DLQ 전송 실패. 원본 페이로드: {Payload}", originalPayload);
        }
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}

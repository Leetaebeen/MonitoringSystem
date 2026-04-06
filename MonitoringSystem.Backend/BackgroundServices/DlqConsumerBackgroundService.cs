using Confluent.Kafka;
using MonitoringSystem.Shared.Models;
using System.Text.Json;

namespace MonitoringSystem.Backend.BackgroundServices;

public class DlqConsumerBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DlqConsumerBackgroundService> _logger;

    public DlqConsumerBackgroundService(IConfiguration configuration, ILogger<DlqConsumerBackgroundService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var dlqTopic = _configuration["Kafka:DlqTopic"] ?? "sensor-topic-dlq";
        var groupId = _configuration["Kafka:DlqGroupId"] ?? "monitoring-backend-dlq-group";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        consumer.Subscribe(dlqTopic);

        _logger.LogInformation("DLQ consumer started. Topic={Topic}, GroupId={GroupId}", dlqTopic, groupId);

        await Task.Run(() =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null) continue;

                    var dlqMessage = JsonSerializer.Deserialize<DlqMessage>(result.Message.Value);
                    if (dlqMessage is null)
                    {
                        _logger.LogWarning("DLQ 메시지 역직렬화 실패. Raw={Raw}", result.Message.Value);
                        continue;
                    }

                    _logger.LogWarning(
                        "DLQ 메시지 감지. OriginalTopic={OriginalTopic}, ErrorType={ErrorType}, Error={Error}, FailedAt={FailedAt}, Payload={Payload}",
                        dlqMessage.OriginalTopic,
                        dlqMessage.ErrorType,
                        dlqMessage.ErrorMessage,
                        dlqMessage.FailedAt,
                        dlqMessage.OriginalPayload);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "DLQ consume error: {Reason}", ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DLQ consumer 예기치 않은 오류");
                }
            }
        }, stoppingToken);

        consumer.Close();
        _logger.LogInformation("DLQ consumer stopped.");
    }
}

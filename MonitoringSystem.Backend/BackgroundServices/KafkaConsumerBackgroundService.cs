using Confluent.Kafka;
using MonitoringSystem.Backend.Data;
using MonitoringSystem.Backend.Services.Kafka;
using MonitoringSystem.Backend.Services.Realtime;
using MonitoringSystem.Shared.Models;
using System.Text.Json;

namespace MonitoringSystem.Backend.BackgroundServices;

public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMonitoringRealtimePublisher _realtimePublisher;
    private readonly KafkaDlqProducer _dlqProducer;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;

    public KafkaConsumerBackgroundService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        IMonitoringRealtimePublisher realtimePublisher,
        KafkaDlqProducer dlqProducer,
        ILogger<KafkaConsumerBackgroundService> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _realtimePublisher = realtimePublisher;
        _dlqProducer = dlqProducer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var topic = _configuration["Kafka:Topic"] ?? "sensor-topic";
        var groupId = _configuration["Kafka:GroupId"] ?? "monitoring-backend-group";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        consumer.Subscribe(topic);

        _logger.LogInformation("Kafka consumer started. Topic={Topic}, GroupId={GroupId}", topic, groupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<Ignore, string>? result = null;
            try
            {
                result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                SensorData? sensorData;
                try
                {
                    sensorData = JsonSerializer.Deserialize<SensorData>(result.Message.Value);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "JSON 역직렬화 실패. DLQ로 이동. Payload={Payload}", result.Message.Value);
                    await _dlqProducer.SendAsync(topic, result.Message.Value, ex, stoppingToken);
                    consumer.Commit(result);
                    continue;
                }

                if (sensorData is null)
                {
                    var ex = new InvalidOperationException("역직렬화 결과가 null입니다.");
                    _logger.LogWarning("Null 역직렬화 결과. DLQ로 이동. Payload={Payload}", result.Message.Value);
                    await _dlqProducer.SendAsync(topic, result.Message.Value, ex, stoppingToken);
                    consumer.Commit(result);
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

                    dbContext.SensorData.Add(sensorData);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    await _realtimePublisher.PublishSensorDataAsync(sensorData, stoppingToken);

                    consumer.Commit(result);

                    _logger.LogInformation(
                        "Saved sensor data. EquipmentId={EquipmentId}, Temperature={Temperature}, LogTime={LogTime}",
                        sensorData.EquipmentId,
                        sensorData.Temperature,
                        sensorData.LogTime);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "DB 저장 또는 SignalR 전송 실패. DLQ로 이동. EquipmentId={EquipmentId}", sensorData.EquipmentId);
                    await _dlqProducer.SendAsync(topic, result.Message.Value, ex, stoppingToken);
                    consumer.Commit(result);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while consuming Kafka messages");
            }
        }

        consumer.Close();
        _logger.LogInformation("Kafka consumer stopped.");
    }
}

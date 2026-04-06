using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MonitoringSystem.Backend.HealthChecks;

public class KafkaHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public KafkaHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var topic = _configuration["Kafka:Topic"] ?? "sensor-topic";

        try
        {
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = bootstrapServers,
                SocketTimeoutMs = 2000
            };

            using var adminClient = new AdminClientBuilder(adminConfig).Build();
            var metadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(2));

            var hasTopic = metadata.Topics.Any(t => string.Equals(t.Topic, topic, StringComparison.Ordinal));
            return Task.FromResult(hasTopic
                ? HealthCheckResult.Healthy("Kafka 연결 정상")
                : HealthCheckResult.Unhealthy($"Kafka 토픽을 찾을 수 없습니다: {topic}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka 연결 확인 중 예외 발생", ex));
        }
    }
}

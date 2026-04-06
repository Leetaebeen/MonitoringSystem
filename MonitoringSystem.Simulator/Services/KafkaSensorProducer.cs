using Confluent.Kafka;
using MonitoringSystem.Shared.Models;
using MonitoringSystem.Simulator.Config;
using System.Text.Json;

namespace MonitoringSystem.Simulator.Services;

public class KafkaSensorProducer : IAsyncDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly SimulatorOptions _options;

    public KafkaSensorProducer(SimulatorOptions options)
    {
        _options = options;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    public Task<DeliveryResult<Null, string>> ProduceAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(sensorData);
        return _producer.ProduceAsync(_options.Topic, new Message<Null, string> { Value = payload }, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(3));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}

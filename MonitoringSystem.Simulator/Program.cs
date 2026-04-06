using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MonitoringSystem.Simulator.Config;
using MonitoringSystem.Simulator.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SimulatorOptions>(builder.Configuration.GetSection("Simulator"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SimulatorOptions>>().Value);
builder.Services.AddSingleton<SensorDataGenerator>();
builder.Services.AddSingleton<KafkaSensorProducer>();

using var host = builder.Build();

var options = host.Services.GetRequiredService<SimulatorOptions>();
var generator = host.Services.GetRequiredService<SensorDataGenerator>();
await using var producer = host.Services.GetRequiredService<KafkaSensorProducer>();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Kafka 시뮬레이터 시작. 종료하려면 Ctrl+C를 누르세요.");

while (!cts.Token.IsCancellationRequested)
{
    var sensorData = generator.Next();

    try
    {
        var result = await producer.ProduceAsync(sensorData, cts.Token);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] sent to {result.TopicPartitionOffset}: {result.Message.Value}");
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (ProduceException<Null, string> ex)
    {
        Console.WriteLine($"Kafka 전송 실패: {ex.Error.Reason}");
    }

    try
    {
        await Task.Delay(options.IntervalMs, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

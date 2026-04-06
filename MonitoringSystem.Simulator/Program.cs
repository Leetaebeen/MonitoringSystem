using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringSystem.Simulator.Config;
using MonitoringSystem.Simulator.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName());

builder.Services.Configure<SimulatorOptions>(builder.Configuration.GetSection("Simulator"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SimulatorOptions>>().Value);
builder.Services.AddSingleton<SensorDataGenerator>();
builder.Services.AddSingleton<KafkaSensorProducer>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var options = host.Services.GetRequiredService<SimulatorOptions>();
var generator = host.Services.GetRequiredService<SensorDataGenerator>();
await using var producer = host.Services.GetRequiredService<KafkaSensorProducer>();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

logger.LogInformation("Kafka 시뮬레이터 시작. 종료하려면 Ctrl+C를 누르세요.");

while (!cts.Token.IsCancellationRequested)
{
    var sensorData = generator.Next();

    try
    {
        var result = await producer.ProduceAsync(sensorData, cts.Token);
        logger.LogInformation(
            "센서 데이터 전송. Offset={Offset} EquipmentId={EquipmentId} Temperature={Temperature}",
            result.TopicPartitionOffset,
            sensorData.EquipmentId,
            sensorData.Temperature);
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (ProduceException<Null, string> ex)
    {
        logger.LogError("Kafka 전송 실패: {Reason}", ex.Error.Reason);
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

logger.LogInformation("시뮬레이터 종료");

}
catch (Exception ex)
{
    Log.Fatal(ex, "시뮬레이터 시작 중 치명적 오류 발생");
}
finally
{
    Log.CloseAndFlush();
}

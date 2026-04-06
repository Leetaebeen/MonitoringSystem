using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MonitoringSystem.Backend.HealthChecks;
using MonitoringSystem.Backend.BackgroundServices;
using MonitoringSystem.Backend.Data;
using MonitoringSystem.Backend.Hubs;
using MonitoringSystem.Backend.Services.Kafka;
using MonitoringSystem.Backend.Services.Monitoring;
using MonitoringSystem.Backend.Services.Realtime;
using Serilog;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
Directory.CreateDirectory(webRoot);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"
});

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId());

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IMonitoringQueryService, MonitoringQueryService>();
builder.Services.AddSingleton<IMonitoringRealtimePublisher, MonitoringRealtimePublisher>();
builder.Services.AddSingleton<KafkaDlqProducer>();
builder.Services.AddHostedService<KafkaConsumerBackgroundService>();
builder.Services.AddHostedService<DlqConsumerBackgroundService>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", failureStatus: HealthStatus.Unhealthy)
    .AddCheck<KafkaHealthCheck>("kafka", failureStatus: HealthStatus.Unhealthy)
    .AddCheck("signalr", () => HealthCheckResult.Healthy("SignalR 엔드포인트 매핑 정상"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
    dbContext.Database.Migrate();
}


app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = entry.Value.Duration.TotalMilliseconds
                }),
            totalDurationMs = report.TotalDuration.TotalMilliseconds
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.MapGet("/", () => "환영합니다! 백엔드 메인 페이지가 정상적으로 작동 중입니다!");

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "백엔드 시작 중 치명적 오류 발생");
}
finally
{
    Log.CloseAndFlush();
}
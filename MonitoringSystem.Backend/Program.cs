using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MonitoringSystem.Backend.HealthChecks;
using MonitoringSystem.Backend.BackgroundServices;
using MonitoringSystem.Backend.Data;
using MonitoringSystem.Backend.Hubs;
using MonitoringSystem.Backend.Models;
using MonitoringSystem.Backend.Services.Auth;
using MonitoringSystem.Backend.Services.Kafka;
using MonitoringSystem.Backend.Services.Monitoring;
using MonitoringSystem.Backend.Services.Realtime;
using Serilog;
using System.Text;
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

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret이 설정되지 않았습니다.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
        // SignalR: 쿼리스트링에서 토큰 추출
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
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

    // 기본 테스트 계정이 없으면 시드
    if (!dbContext.Users.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var seedAccounts = new[]
        {
            ("admin",    "admin123", "Admin"),
            ("operator", "op123",    "Operator"),
        };
        foreach (var (username, password, role) in seedAccounts)
        {
            var user = new AppUser { Username = username, Role = role };
            user.PasswordHash = hasher.HashPassword(user, password);
            dbContext.Users.Add(user);
        }
        dbContext.SaveChanges();
        Log.Information("기본 테스트 계정 시드 완료 (admin / operator / viewer)");
    }
}


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
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
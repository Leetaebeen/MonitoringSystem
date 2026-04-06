using Microsoft.AspNetCore.Components.Authorization;
using MonitoringSystem.Frontend.Components;
using MonitoringSystem.Frontend.Services.Auth;
using MonitoringSystem.Frontend.Services.Monitoring;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, TokenAuthenticationStateProvider>();
builder.Services.AddScoped<TokenDelegatingHandler>();

var backendBaseUrl = builder.Configuration["BackendApi:BaseUrl"] ?? "https://localhost:7280";
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
}).AddHttpMessageHandler<TokenDelegatingHandler>();

builder.Services.AddScoped<MonitoringApiClient>();
builder.Services.AddScoped<MonitoringHubClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "프론트엔드 시작 중 치명적 오류 발생");
}
finally
{
    Log.CloseAndFlush();
}

using MonitoringSystem.Frontend.Components;
using MonitoringSystem.Frontend.Services.Monitoring;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var backendBaseUrl = builder.Configuration["BackendApi:BaseUrl"] ?? "https://localhost:7280";
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
});
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

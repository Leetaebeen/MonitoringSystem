using Microsoft.AspNetCore.SignalR.Client;
using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Frontend.Services.Monitoring;

public class MonitoringHubClient : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private HubConnection? _hubConnection;

    public MonitoringHubClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StartAsync(
        Func<SensorData, Task> onSensorData,
        Func<string, Task> onConnectionStateChanged)
    {
        if (_hubConnection is not null)
        {
            return;
        }

        var backendBaseUrl = (_configuration["BackendApi:BaseUrl"] ?? "https://localhost:7280").TrimEnd('/');

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{backendBaseUrl}/hubs/monitoring")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<SensorData>("ReceiveSensorData", onSensorData);

        _hubConnection.Reconnecting += _ => onConnectionStateChanged("재연결 중");
        _hubConnection.Reconnected += _ => onConnectionStateChanged("연결됨");
        _hubConnection.Closed += _ => onConnectionStateChanged("연결 끊김");

        await _hubConnection.StartAsync();
        await onConnectionStateChanged("연결됨");
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}

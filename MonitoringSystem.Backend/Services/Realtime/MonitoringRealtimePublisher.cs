using Microsoft.AspNetCore.SignalR;
using MonitoringSystem.Backend.Hubs;
using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Backend.Services.Realtime;

public class MonitoringRealtimePublisher : IMonitoringRealtimePublisher
{
    private readonly IHubContext<MonitoringHub> _hubContext;

    public MonitoringRealtimePublisher(IHubContext<MonitoringHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishSensorDataAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync("ReceiveSensorData", sensorData, cancellationToken);
    }
}

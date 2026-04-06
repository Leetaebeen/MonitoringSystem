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

    public async Task PublishSensorDataAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        // 전체 구독 그룹
        var allTask = _hubContext.Clients.Group("all")
            .SendAsync("ReceiveSensorData", sensorData, cancellationToken);

        // 설비별 그룹
        var equipmentTask = _hubContext.Clients.Group($"equipment:{sensorData.EquipmentId}")
            .SendAsync("ReceiveSensorData", sensorData, cancellationToken);

        // 라인별 그룹
        var lineTask = _hubContext.Clients.Group($"line:{sensorData.LineId}")
            .SendAsync("ReceiveSensorData", sensorData, cancellationToken);

        await Task.WhenAll(allTask, equipmentTask, lineTask);
    }
}

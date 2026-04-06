using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Backend.Services.Realtime;

public interface IMonitoringRealtimePublisher
{
    Task PublishSensorDataAsync(SensorData sensorData, CancellationToken cancellationToken = default);
}

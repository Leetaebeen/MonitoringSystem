using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Backend.Services.Monitoring;

public interface IMonitoringQueryService
{
    Task<List<SensorData>> GetLatestAsync(int take, string? lineId, string? equipmentId, CancellationToken cancellationToken = default);
    Task<HistoryQueryResult> GetHistoryAsync(DateTime fromUtc, DateTime toUtc, string? lineId, string? equipmentId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<SensorData>> GetLatestAlertsAsync(int take, CancellationToken cancellationToken = default);
}

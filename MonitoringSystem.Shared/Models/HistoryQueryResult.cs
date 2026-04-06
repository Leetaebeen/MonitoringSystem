namespace MonitoringSystem.Shared.Models;

public class HistoryQueryResult
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<SensorData> Items { get; set; } = [];
}

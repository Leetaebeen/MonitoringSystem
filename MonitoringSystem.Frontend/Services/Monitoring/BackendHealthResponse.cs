namespace MonitoringSystem.Frontend.Services.Monitoring;

public class BackendHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, BackendHealthCheckEntry> Checks { get; set; } = [];
    public double TotalDurationMs { get; set; }
}

public class BackendHealthCheckEntry
{
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double DurationMs { get; set; }
}

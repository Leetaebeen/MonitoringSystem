namespace MonitoringSystem.Shared.Models;

public class DlqMessage
{
    public string OriginalTopic { get; set; } = string.Empty;
    public string OriginalPayload { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

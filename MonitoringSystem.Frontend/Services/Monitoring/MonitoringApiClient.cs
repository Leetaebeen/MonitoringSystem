using MonitoringSystem.Shared.Models;
using MonitoringSystem.Frontend.Services.Auth;

namespace MonitoringSystem.Frontend.Services.Monitoring;

public class MonitoringApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthService _authService;

    public MonitoringApiClient(IHttpClientFactory httpClientFactory, AuthService authService)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
    }

    private HttpClient CreateBackendClient()
    {
        var client = _httpClientFactory.CreateClient("BackendApi");

        if (!string.IsNullOrWhiteSpace(_authService.Token))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.Token);
        }
        else
        {
            client.DefaultRequestHeaders.Authorization = null;
        }

        return client;
    }

    public async Task<List<SensorData>?> GetLatestAsync(
        int take,
        string? lineId = null,
        string? equipmentId = null,
        CancellationToken cancellationToken = default)
    {
        var client = CreateBackendClient();

        var queryParts = new List<string> { $"take={take}" };

        if (!string.IsNullOrWhiteSpace(lineId))
        {
            queryParts.Add($"lineId={Uri.EscapeDataString(lineId)}");
        }

        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            queryParts.Add($"equipmentId={Uri.EscapeDataString(equipmentId)}");
        }

        var query = string.Join("&", queryParts);
        return await client.GetFromJsonAsync<List<SensorData>>($"api/monitoring/latest?{query}", cancellationToken);
    }

    public async Task<List<SensorData>?> GetLatestAlertsAsync(int take, CancellationToken cancellationToken = default)
    {
        var client = CreateBackendClient();
        return await client.GetFromJsonAsync<List<SensorData>>($"api/monitoring/alerts?take={take}", cancellationToken);
    }

    public async Task<HistoryQueryResult?> GetHistoryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        string? lineId,
        string? equipmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var client = CreateBackendClient();

        var queryParts = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (fromUtc.HasValue)
        {
            queryParts.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("o"))}");
        }

        if (toUtc.HasValue)
        {
            queryParts.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("o"))}");
        }

        if (!string.IsNullOrWhiteSpace(lineId))
        {
            queryParts.Add($"lineId={Uri.EscapeDataString(lineId)}");
        }

        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            queryParts.Add($"equipmentId={Uri.EscapeDataString(equipmentId)}");
        }

        var query = string.Join("&", queryParts);
        return await client.GetFromJsonAsync<HistoryQueryResult>($"api/monitoring/history?{query}", cancellationToken);
    }

    public async Task<BackendHealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateBackendClient();
        return await client.GetFromJsonAsync<BackendHealthResponse>("health", cancellationToken);
    }
}

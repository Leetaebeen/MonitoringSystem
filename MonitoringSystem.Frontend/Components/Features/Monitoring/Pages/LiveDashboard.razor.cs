using Microsoft.AspNetCore.Components;
using MonitoringSystem.Frontend.Services.Auth;
using MonitoringSystem.Frontend.Services.Monitoring;
using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Frontend.Components.Features.Monitoring.Pages;

public partial class LiveDashboard : IAsyncDisposable
{
    [Inject] private MonitoringApiClient MonitoringApiClient { get; set; } = default!;
    [Inject] private MonitoringHubClient MonitoringHubClient { get; set; } = default!;
    [Inject] private AuthService AuthService { get; set; } = default!;

    private const int MaxRows = 20;
    private const int DefaultIntervalSeconds = 5;

    private string _connectionState = "연결 중";
    private string? _errorMessage;
    private bool _isLoading = true;
    private SensorData? _latestData;
    private readonly List<SensorData> _recentData = [];
    private DateTime _lastRealtimeAppliedAtUtc = DateTime.MinValue;

    private string? _selectedEquipmentId;
    private string? _selectedLineId;
    private List<string> _availableEquipmentIds = [];
    private List<string> _availableLineIds = [];

    private string _currentGroupLabel => !string.IsNullOrEmpty(_selectedEquipmentId)
        ? $"설비 {_selectedEquipmentId}"
        : $"라인 {_selectedLineId}";

    private int AlertCount => _recentData.Count(x => x.IsAlert);
    private double AverageTemperature => _recentData.Count == 0 ? 0 : _recentData.Average(x => x.Temperature);
    private double AverageVibration => _recentData.Count == 0 ? 0 : _recentData.Average(x => x.VibrationMmS);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await AuthService.TryRestoreFromSessionAsync();

        try
        {
            await LoadInitialDataAsync();
            await ConnectHubAsync();
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var latest = await MonitoringApiClient.GetLatestAsync(
                MaxRows,
                _selectedLineId,
                _selectedEquipmentId,
                cts.Token);

            _latestData = null;
            _recentData.Clear();
            if (latest is { Count: > 0 })
            {
                _recentData.AddRange(latest.Take(MaxRows));
                _latestData = _recentData[0];

                if (_availableEquipmentIds.Count == 0)
                    _availableEquipmentIds = latest.Select(x => x.EquipmentId).Distinct().Order().ToList();
                if (_availableLineIds.Count == 0)
                    _availableLineIds = latest.Select(x => x.LineId).Distinct().Order().ToList();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"초기 데이터 로딩 실패: {ex.Message}";
        }
    }

    private async Task ConnectHubAsync()
    {
        try
        {
            await MonitoringHubClient.StartAsync(
                async data =>
                {
                    var now = DateTime.UtcNow;
                    if (now - _lastRealtimeAppliedAtUtc < TimeSpan.FromSeconds(DefaultIntervalSeconds))
                        return;

                    _lastRealtimeAppliedAtUtc = now;
                    ApplyRealtimeData(data);
                    await InvokeAsync(StateHasChanged);
                },
                async state =>
                {
                    _connectionState = state;
                    await InvokeAsync(StateHasChanged);
                });
        }
        catch (Exception ex)
        {
            _connectionState = "백엔드 미연결";
            _errorMessage = string.IsNullOrWhiteSpace(_errorMessage)
                ? $"실시간 연결 실패: {ex.Message}"
                : $"{_errorMessage} / 실시간 연결 실패: {ex.Message}";
        }
    }

    private async Task OnEquipmentFilterChangedAsync(ChangeEventArgs e)
    {
        _selectedEquipmentId = e.Value?.ToString();
        _selectedLineId = null;

        var groupName = string.IsNullOrEmpty(_selectedEquipmentId) ? "all" : $"equipment:{_selectedEquipmentId}";
        await MonitoringHubClient.SubscribeToGroupAsync(groupName);
        await LoadInitialDataAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnLineFilterChangedAsync(ChangeEventArgs e)
    {
        _selectedLineId = e.Value?.ToString();
        _selectedEquipmentId = null;

        var groupName = string.IsNullOrEmpty(_selectedLineId) ? "all" : $"line:{_selectedLineId}";
        await MonitoringHubClient.SubscribeToGroupAsync(groupName);
        await LoadInitialDataAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ResetFilterAsync()
    {
        _selectedEquipmentId = null;
        _selectedLineId = null;

        await MonitoringHubClient.SubscribeToGroupAsync("all");
        await LoadInitialDataAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void ApplyRealtimeData(SensorData data)
    {
        _latestData = data;
        _recentData.Insert(0, data);

        if (_recentData.Count > MaxRows)
            _recentData.RemoveAt(_recentData.Count - 1);
    }

    private string GetConnectionBadgeClass()
        => _connectionState switch
        {
            "연결됨"      => "bg-success",
            "재연결 중"   => "bg-warning text-dark",
            "연결 끊김"   => "bg-secondary",
            "백엔드 미연결" => "bg-danger",
            _             => "bg-info text-dark"
        };

    public async ValueTask DisposeAsync()
    {
        await MonitoringHubClient.DisposeAsync();
    }
}

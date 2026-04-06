using Microsoft.AspNetCore.SignalR.Client;
using MonitoringSystem.Frontend.Services.Auth;
using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Frontend.Services.Monitoring;

public class MonitoringHubClient : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;
    private HubConnection? _hubConnection;
    private string? _currentGroup; // "all" | "equipment:{id}" | "line:{id}"

    public MonitoringHubClient(IConfiguration configuration, AuthService authService)
    {
        _configuration = configuration;
        _authService = authService;
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
            .WithUrl($"{backendBaseUrl}/hubs/monitoring", options =>
            {
                // JWT 토큰을 쿼리스트링으로 전달 (WebSocket은 헤더 불가)
                options.AccessTokenProvider = () => Task.FromResult(_authService.Token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<SensorData>("ReceiveSensorData", onSensorData);

        _hubConnection.Reconnecting += _ => onConnectionStateChanged("재연결 중");
        _hubConnection.Reconnected += _ => onConnectionStateChanged("연결됨");
        _hubConnection.Closed += _ => onConnectionStateChanged("연결 끊김");

        await _hubConnection.StartAsync();
        _currentGroup = "all"; // 서버의 OnConnectedAsync에서 자동 가입됨
        await onConnectionStateChanged("연결됨");
    }

    /// <summary>
    /// 특정 그룹만 구독합니다. 기존 구독 그룹은 탈퇴합니다.
    /// groupName: "all" | "equipment:{EquipmentId}" | "line:{LineId}"
    /// </summary>
    public async Task SubscribeToGroupAsync(string groupName)
    {
        if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
            return;

        if (_currentGroup == groupName)
            return;

        if (_currentGroup is not null)
        {
            await _hubConnection.InvokeAsync("LeaveGroupAsync", _currentGroup);
        }

        await _hubConnection.InvokeAsync("JoinGroupAsync", groupName);
        _currentGroup = groupName;
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

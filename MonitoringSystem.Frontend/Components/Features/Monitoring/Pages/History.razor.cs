using Microsoft.AspNetCore.Components;
using MonitoringSystem.Frontend.Services.Auth;
using MonitoringSystem.Frontend.Services.Monitoring;
using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Frontend.Components.Features.Monitoring.Pages;

public partial class History
{
    [Inject] private MonitoringApiClient MonitoringApiClient { get; set; } = default!;
    [Inject] private AuthService AuthService { get; set; } = default!;

    // 필터 상태
    private DateOnly _fromDate;
    private TimeOnly _fromTime;
    private DateOnly _toDate;
    private TimeOnly _toTime;
    private string? _lineIdFilter;
    private string? _equipmentIdFilter;

    // 적용된 필터 상태 (조회 버튼을 눌렀을 때만 갱신)
    private string? _appliedLineIdFilter;
    private string? _appliedEquipmentIdFilter;

    // 페이징 및 UI 상태
    private int _page = 1;
    private int _pageSize = 20;
    private int _totalCount;
    private int _totalPages;
    private bool _isLoading;
    private string? _errorMessage;

    // 데이터 보관
    private readonly List<SensorData> _items = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await AuthService.TryRestoreFromSessionAsync();
        ApplyQuickRange(1);
        await LoadHistoryAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadHistoryAsync()
    {
        if (_isLoading) return;

        _isLoading = true;
        _errorMessage = null;

        try
        {
            if (!TryGetRangeUtc(out var fromUtc, out var toUtc))
            {
                ShowError("시작/종료 시각 형식이 올바르지 않습니다.");
                return;
            }

            if (fromUtc > toUtc)
            {
                ShowError("시작 시각은 종료 시각보다 이전이어야 합니다.");
                return;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var result = await MonitoringApiClient.GetHistoryAsync(
                fromUtc, toUtc, _appliedLineIdFilter, _appliedEquipmentIdFilter, _page, _pageSize, cts.Token);

            _items.Clear();

            if (result is not null)
            {
                _page = result.Page;
                _totalCount = result.TotalCount;
                _totalPages = result.TotalPages;
                _items.AddRange(result.Items);
            }
        }
        catch (OperationCanceledException)
        {
            ShowError("데이터 조회 시간이 초과되었습니다. 조회 범위를 줄여보세요.");
        }
        catch (Exception ex)
        {
            ShowError($"조회 중 오류가 발생했습니다: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task ApplyFilterAsync()
    {
        _appliedLineIdFilter = _lineIdFilter?.Trim();
        _appliedEquipmentIdFilter = _equipmentIdFilter?.Trim();
        _page = 1;
        await LoadHistoryAsync();
    }

    private async Task ResetFilterAsync()
    {
        ApplyQuickRange(1);
        _lineIdFilter = null;
        _equipmentIdFilter = null;
        _appliedLineIdFilter = null;
        _appliedEquipmentIdFilter = null;
        _page = 1;
        await LoadHistoryAsync();
    }

    private async Task ApplyQuickRangeAsync(int hours)
    {
        ApplyQuickRange(hours);
        _page = 1;
        await LoadHistoryAsync();
    }

    private async Task PrevPageAsync()
    {
        if (_page > 1) { _page--; await LoadHistoryAsync(); }
    }

    private async Task NextPageAsync()
    {
        if (_page < Math.Max(_totalPages, 1)) { _page++; await LoadHistoryAsync(); }
    }

    private async Task GoToPageAsync(int pageNumber)
    {
        if (pageNumber >= 1 && pageNumber <= _totalPages && pageNumber != _page)
        {
            _page = pageNumber;
            await LoadHistoryAsync();
        }
    }

    private void ApplyQuickRange(int hours)
    {
        var now = DateTime.Now;
        var past = now.AddHours(-Math.Max(hours, 1));

        _fromDate = DateOnly.FromDateTime(past);
        _fromTime = TimeOnly.FromDateTime(past);
        _toDate = DateOnly.FromDateTime(now);
        _toTime = TimeOnly.FromDateTime(now);
    }

    private bool TryGetRangeUtc(out DateTime fromUtc, out DateTime toUtc)
    {
        try
        {
            var fromLocal = _fromDate.ToDateTime(_fromTime, DateTimeKind.Local);
            var toLocal = _toDate.ToDateTime(_toTime, DateTimeKind.Local);
            fromUtc = fromLocal.ToUniversalTime();
            toUtc = toLocal.ToUniversalTime();
            return true;
        }
        catch
        {
            fromUtc = default;
            toUtc = default;
            return false;
        }
    }

    private string GetFilterLabel()
    {
        var line = string.IsNullOrWhiteSpace(_appliedLineIdFilter) ? "전체" : _appliedLineIdFilter;
        var equipment = string.IsNullOrWhiteSpace(_appliedEquipmentIdFilter) ? "전체" : _appliedEquipmentIdFilter;
        return $"라인: {line} / 설비: {equipment}";
    }

    private IEnumerable<int> GetVisiblePageButtons()
    {
        var max = Math.Max(_totalPages, 1);
        var start = Math.Max(1, _page - 2);
        var end = Math.Min(max, start + 4);

        if (end - start < 4)
            start = Math.Max(1, end - 4);

        return Enumerable.Range(start, end - start + 1);
    }

    private void ShowError(string message)
    {
        _errorMessage = message;
        _items.Clear();
        _totalCount = 0;
        _totalPages = 0;
    }
}

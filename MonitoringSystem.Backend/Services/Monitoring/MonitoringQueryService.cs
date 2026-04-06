using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Backend.Data;
using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Backend.Services.Monitoring;

public class MonitoringQueryService : IMonitoringQueryService
{
    private readonly MonitoringDbContext _dbContext;

    public MonitoringQueryService(MonitoringDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SensorData>> GetLatestAsync(int take, string? lineId, string? equipmentId, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        lineId = lineId?.Trim();
        equipmentId = equipmentId?.Trim();

        IQueryable<SensorData> query = _dbContext.SensorData.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(lineId))
        {
            query = query.Where(x => EF.Functions.Like(x.LineId, $"%{lineId}%"));
        }

        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            query = query.Where(x => EF.Functions.Like(x.EquipmentId, $"%{equipmentId}%"));
        }

        return await query
            .OrderByDescending(x => x.LogTime)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<HistoryQueryResult> GetHistoryAsync(DateTime fromUtc, DateTime toUtc, string? lineId, string? equipmentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 500);
        lineId = lineId?.Trim();
        equipmentId = equipmentId?.Trim();

        IQueryable<SensorData> query = _dbContext.SensorData
            .AsNoTracking()
            .Where(x => x.LogTime >= fromUtc && x.LogTime <= toUtc);

        if (!string.IsNullOrWhiteSpace(lineId))
        {
            query = query.Where(x => EF.Functions.Like(x.LineId, $"%{lineId}%"));
        }

        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            query = query.Where(x => EF.Functions.Like(x.EquipmentId, $"%{equipmentId}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var items = await query
            .OrderByDescending(x => x.LogTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new HistoryQueryResult
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items
        };
    }

    public async Task<List<SensorData>> GetLatestAlertsAsync(int take, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);

        return await _dbContext.SensorData
            .AsNoTracking()
            .Where(x => x.IsAlert)
            .OrderByDescending(x => x.LogTime)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MonitoringSystem.Backend.Services.Monitoring;
using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringQueryService _queryService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(IMonitoringQueryService queryService, ILogger<MonitoringController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<IEnumerable<SensorData>>> GetLatest(
        [FromQuery] int take = 20,
        [FromQuery] string? lineId = null,
        [FromQuery] string? equipmentId = null)
    {
        try
        {
            var items = await _queryService.GetLatestAsync(
                take,
                lineId,
                equipmentId,
                HttpContext.RequestAborted);

            return Ok(items);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error in GetLatest");
            return Problem(title: "데이터베이스 오류", detail: "DB 스키마를 확인하세요.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<HistoryQueryResult>> GetHistory(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? lineId = null,
        [FromQuery] string? equipmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var from = fromUtc ?? DateTime.UtcNow.AddHours(-1);
            var to = toUtc ?? DateTime.UtcNow;

            _logger.LogInformation(
                "GetHistory called. from={FromUtc}, to={ToUtc}, lineId={LineId}, equipmentId={EquipmentId}, page={Page}, pageSize={PageSize}",
                from,
                to,
                lineId,
                equipmentId,
                page,
                pageSize);

            var items = await _queryService.GetHistoryAsync(
                from,
                to,
                lineId,
                equipmentId,
                page,
                pageSize,
                HttpContext.RequestAborted);

            _logger.LogInformation(
                "GetHistory result. page={Page}, totalPages={TotalPages}, totalCount={TotalCount}, itemCount={ItemCount}",
                items.Page,
                items.TotalPages,
                items.TotalCount,
                items.Items.Count);

            return Ok(items);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error in GetHistory");
            return Problem(title: "데이터베이스 오류", detail: "DB 스키마를 확인하세요.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<SensorData>>> GetLatestAlerts([FromQuery] int take = 20)
    {
        try
        {
            var alerts = await _queryService.GetLatestAlertsAsync(take, HttpContext.RequestAborted);

            return Ok(alerts);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error in GetLatestAlerts");
            return Problem(title: "데이터베이스 오류", detail: "DB 스키마를 확인하세요.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

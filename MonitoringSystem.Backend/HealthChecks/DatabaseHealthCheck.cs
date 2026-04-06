using Microsoft.Extensions.Diagnostics.HealthChecks;
using MonitoringSystem.Backend.Data;

namespace MonitoringSystem.Backend.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly MonitoringDbContext _dbContext;

    public DatabaseHealthCheck(MonitoringDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("데이터베이스 연결 정상")
                : HealthCheckResult.Unhealthy("데이터베이스 연결 실패");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("데이터베이스 연결 확인 중 예외 발생", ex);
        }
    }
}

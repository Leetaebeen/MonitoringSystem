using Microsoft.EntityFrameworkCore;
using MonitoringSystem.Shared.Models;

namespace MonitoringSystem.Backend.Data;

public class MonitoringDbContext : DbContext
{
    public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options)
        : base(options)
    {
    }

    public DbSet<SensorData> SensorData { get; set; }
}

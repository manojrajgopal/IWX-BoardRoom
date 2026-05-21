using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace IWX.SchedulerEngine.Domain;

public sealed class ScheduledJob
{
    [Key, MaxLength(128)]
    public string Key { get; set; } = "";

    [MaxLength(256)] public string Name { get; set; } = "";
    [MaxLength(64)]  public string CronExpression { get; set; } = "";
    [MaxLength(64)]  public string TargetDepartment { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastFiredAtUtc { get; set; }
    public long FireCount { get; set; }
}

public sealed class SchedulerDbContext : DbContext
{
    public SchedulerDbContext(DbContextOptions<SchedulerDbContext> opts) : base(opts) { }
    public DbSet<ScheduledJob> Jobs => Set<ScheduledJob>();
}

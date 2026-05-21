using IWX.CeoAgent.Domain;
using Microsoft.EntityFrameworkCore;

namespace IWX.CeoAgent.Infrastructure;

public sealed class CeoDbContext(DbContextOptions<CeoDbContext> options) : DbContext(options)
{
    public DbSet<BoardTask> BoardTasks => Set<BoardTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var bt = modelBuilder.Entity<BoardTask>();
        bt.ToTable("BoardTasks");
        bt.HasKey(x => x.Id);
        bt.Property(x => x.Title).HasMaxLength(256).IsRequired();
        bt.Property(x => x.TargetDepartment).HasMaxLength(64).IsRequired();
        bt.Property(x => x.Priority).HasMaxLength(32);
        bt.Property(x => x.CreatedBy).HasMaxLength(64);
        bt.Property(x => x.Status).HasConversion<int>();
        bt.HasIndex(x => new { x.Status, x.TargetDepartment });
    }
}

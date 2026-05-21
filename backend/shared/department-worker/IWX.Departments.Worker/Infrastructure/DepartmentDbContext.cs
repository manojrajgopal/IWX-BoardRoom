using IWX.Departments.Worker.Domain;
using Microsoft.EntityFrameworkCore;

namespace IWX.Departments.Worker.Infrastructure;

public sealed class DepartmentDbContext(DbContextOptions<DepartmentDbContext> options) : DbContext(options)
{
    public DbSet<DepartmentTask> Tasks => Set<DepartmentTask>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<DepartmentTask>(e =>
        {
            e.ToTable("Tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.Property(x => x.Priority).HasMaxLength(32);
            e.Property(x => x.ApprovedBy).HasMaxLength(128);
            e.Property(x => x.ResultSummary).HasMaxLength(1024);
        });
    }
}

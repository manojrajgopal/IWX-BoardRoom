using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace IWX.ApprovalEngine.Domain;

public enum ApprovalStatus { Pending, Approved, Rejected, Expired }

public sealed class ApprovalRequest
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(256)] public string Subject { get; set; } = "";
    [MaxLength(128)] public string Requester { get; set; } = "";
    [MaxLength(64)]  public string TargetDepartment { get; set; } = "";
    [MaxLength(32)]  public string Priority { get; set; } = "Normal";
    public string PayloadJson { get; set; } = "{}";

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    [MaxLength(128)] public string? DecidedBy { get; set; }
    [MaxLength(1024)] public string? Comment { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
}

public sealed class ApprovalDbContext : DbContext
{
    public ApprovalDbContext(DbContextOptions<ApprovalDbContext> opts) : base(opts) { }
    public DbSet<ApprovalRequest> Approvals => Set<ApprovalRequest>();
}

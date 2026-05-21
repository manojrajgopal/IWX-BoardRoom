using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace IWX.AuthService.Domain;

public sealed class IwxUser
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(128)] public string Username { get; set; } = "";
    [MaxLength(256)] public string Email { get; set; } = "";
    [MaxLength(64)]  public string TenantId { get; set; } = "default";
    [MaxLength(512)] public string PasswordHash { get; set; } = "";
    [MaxLength(128)] public string PasswordSalt { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
    public List<IwxUserRole> Roles { get; set; } = new();
}

public sealed class IwxRole
{
    [Key, MaxLength(64)] public string Name { get; set; } = "";
    [MaxLength(256)] public string Description { get; set; } = "";
}

public sealed class IwxUserRole
{
    public Guid UserId { get; set; }
    public IwxUser User { get; set; } = default!;
    [MaxLength(64)] public string RoleName { get; set; } = "";
    public IwxRole Role { get; set; } = default!;
}

public sealed class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> opts) : base(opts) { }
    public DbSet<IwxUser> Users => Set<IwxUser>();
    public DbSet<IwxRole> Roles => Set<IwxRole>();
    public DbSet<IwxUserRole> UserRoles => Set<IwxUserRole>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<IwxUser>().HasIndex(u => u.Username).IsUnique();
        mb.Entity<IwxUser>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<IwxUserRole>().HasKey(x => new { x.UserId, x.RoleName });
        mb.Entity<IwxUserRole>().HasOne(x => x.User).WithMany(u => u.Roles).HasForeignKey(x => x.UserId);
        mb.Entity<IwxUserRole>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleName);
    }
}

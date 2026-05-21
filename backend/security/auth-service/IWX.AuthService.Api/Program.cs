using System.IdentityModel.Tokens.Jwt;
using IWX.AuthService.Domain;
using IWX.AuthService.Infrastructure;
using IWX.Common.Observability;
using IWX.Contracts.Security;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.auth-service");

builder.Services.AddDbContext<AuthDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtIssuer>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        var rmq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(rmq["Host"] ?? "localhost", h =>
        {
            h.Username(rmq["User"] ?? "guest");
            h.Password(rmq["Pass"] ?? "guest");
        });
        cfg.Message<AuthIssuedEvent>(m => m.SetEntityName(SecurityQueues.AuthIssued));
        cfg.Message<AccessDeniedEvent>(m => m.SetEntityName(SecurityQueues.AccessDenied));
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwt = new JwtIssuer(builder.Configuration);
        opt.TokenValidationParameters = jwt.ValidationParameters;
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200", "http://localhost:8080")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    var attempts = 0;
    while (attempts++ < 20)
    {
        try { db.Database.EnsureCreated(); break; } catch { Thread.Sleep(2000); }
    }

    // Seed default roles + bootstrap CEO user (idempotent)
    foreach (var role in new[]
    {
        ("ceo", "Chief Executive Officer — full access"),
        ("admin", "Platform administrator"),
        ("director", "Department director"),
        ("agent", "AI agent service principal"),
        ("user", "Standard human user"),
    })
    {
        if (!db.Roles.Any(r => r.Name == role.Item1))
            db.Roles.Add(new IwxRole { Name = role.Item1, Description = role.Item2 });
    }
    db.SaveChanges();

    if (!db.Users.Any())
    {
        var (h, s) = hasher.Hash(builder.Configuration["Bootstrap:CeoPassword"] ?? "ChangeMe!2026");
        var ceo = new IwxUser
        {
            Username = "ceo",
            Email = "ceo@infinitewavex.local",
            TenantId = "default",
            PasswordHash = h,
            PasswordSalt = s,
        };
        ceo.Roles.Add(new IwxUserRole { UserId = ceo.Id, RoleName = "ceo" });
        ceo.Roles.Add(new IwxUserRole { UserId = ceo.Id, RoleName = "admin" });
        db.Users.Add(ceo);
        db.SaveChanges();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "auth" }));
app.MapGet("/service", () => Results.Ok(SecurityRegistry.AuthService));

app.MapPost("/users", async (CreateUserBody body, AuthDbContext db, PasswordHasher hasher, CancellationToken ct) =>
{
    if (await db.Users.AnyAsync(u => u.Username == body.Username, ct))
        return Results.Conflict(new { error = "username exists" });
    var (h, s) = hasher.Hash(body.Password);
    var u = new IwxUser
    {
        Username = body.Username,
        Email = body.Email,
        TenantId = body.TenantId ?? "default",
        PasswordHash = h,
        PasswordSalt = s,
    };
    foreach (var r in body.Roles ?? new[] { "user" })
        u.Roles.Add(new IwxUserRole { UserId = u.Id, RoleName = r });
    db.Users.Add(u);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { u.Id, u.Username, u.Email, u.TenantId, roles = u.Roles.Select(r => r.RoleName) });
});

app.MapGet("/users", async (AuthDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Users.Include(u => u.Roles).Select(u => new
    {
        u.Id, u.Username, u.Email, u.TenantId, u.Enabled, u.CreatedAtUtc, u.LastLoginAtUtc,
        roles = u.Roles.Select(r => r.RoleName)
    }).ToListAsync(ct)));

app.MapPost("/login", async (LoginBody body, AuthDbContext db, PasswordHasher hasher, JwtIssuer jwt, IPublishEndpoint bus, CancellationToken ct) =>
{
    var u = await db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Username == body.Username, ct);
    if (u is null || !u.Enabled)
    {
        await bus.Publish(new AccessDeniedEvent(body.Username, "auth", "login", "user-not-found-or-disabled", DateTime.UtcNow), ct);
        return Results.Unauthorized();
    }
    if (!hasher.Verify(body.Password, u.PasswordHash, u.PasswordSalt))
    {
        await bus.Publish(new AccessDeniedEvent(body.Username, "auth", "login", "bad-password", DateTime.UtcNow), ct);
        return Results.Unauthorized();
    }

    var roles = u.Roles.Select(r => r.RoleName).ToList();
    var (token, tokenId, expiresUtc) = jwt.Issue(u, roles);
    u.LastLoginAtUtc = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);

    await bus.Publish(new AuthIssuedEvent(u.Username, u.TenantId, roles, tokenId, DateTime.UtcNow, expiresUtc), ct);
    return Results.Ok(new { token, expiresUtc, roles, tenant = u.TenantId, subject = u.Username });
});

app.MapPost("/validate", (ValidateBody body, JwtIssuer jwt) =>
{
    try
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(body.Token, jwt.ValidationParameters, out var _);
        return Results.Ok(new
        {
            valid = true,
            subject = principal.Identity?.Name,
            claims = principal.Claims.Select(c => new { c.Type, c.Value })
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { valid = false, error = ex.Message });
    }
});

app.MapGet("/me", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();
    return Results.Ok(new
    {
        subject = ctx.User.Identity!.Name,
        claims = ctx.User.Claims.Select(c => new { c.Type, c.Value })
    });
}).RequireAuthorization();

app.Run();

public sealed record CreateUserBody(string Username, string Email, string Password, string? TenantId, string[]? Roles);
public sealed record LoginBody(string Username, string Password);
public sealed record ValidateBody(string Token);

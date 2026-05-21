using IWX.ApprovalEngine.Domain;
using IWX.Common.Observability;
using IWX.Contracts.Automation;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.approval-engine");

builder.Services.AddDbContext<ApprovalDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

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
        cfg.Message<ApprovalRequestedEvent>(m => m.SetEntityName(AutomationQueues.ApprovalRequested));
        cfg.Message<ApprovalDecidedEvent>(m => m.SetEntityName(AutomationQueues.ApprovalDecided));
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200", "http://localhost:8080")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApprovalDbContext>();
    var attempts = 0;
    while (attempts++ < 20)
    {
        try { db.Database.EnsureCreated(); break; } catch { Thread.Sleep(2000); }
    }
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok", engine = "approval" }));
app.MapGet("/engine", () => Results.Ok(EngineRegistry.ApprovalEngine));

app.MapPost("/approvals", async (
    ApprovalRequest req, ApprovalDbContext db, IPublishEndpoint bus, CancellationToken ct) =>
{
    req.Id = Guid.NewGuid();
    req.Status = ApprovalStatus.Pending;
    req.RequestedAtUtc = DateTime.UtcNow;
    req.DecidedAtUtc = null;
    req.DecidedBy = null;
    req.Comment = null;
    db.Approvals.Add(req);
    await db.SaveChangesAsync(ct);

    await bus.Publish(new ApprovalRequestedEvent(
        req.Id, req.Subject, req.Requester, req.TargetDepartment, req.Priority,
        req.PayloadJson, req.RequestedAtUtc), ct);

    return Results.Ok(req);
});

app.MapGet("/approvals", async (string? status, ApprovalDbContext db, CancellationToken ct) =>
{
    IQueryable<ApprovalRequest> q = db.Approvals.OrderByDescending(a => a.RequestedAtUtc);
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, true, out var s))
        q = q.Where(a => a.Status == s);
    return Results.Ok(await q.Take(200).ToListAsync(ct));
});

app.MapGet("/approvals/pending", async (ApprovalDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Approvals
        .Where(a => a.Status == ApprovalStatus.Pending)
        .OrderByDescending(a => a.RequestedAtUtc).ToListAsync(ct)));

app.MapGet("/approvals/{id:guid}", async (Guid id, ApprovalDbContext db, CancellationToken ct) =>
{
    var a = await db.Approvals.FirstOrDefaultAsync(x => x.Id == id, ct);
    return a is null ? Results.NotFound() : Results.Ok(a);
});

app.MapPost("/approvals/{id:guid}/decide", async (
    Guid id, ApprovalDecision body, ApprovalDbContext db, IPublishEndpoint bus, CancellationToken ct) =>
{
    var a = await db.Approvals.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (a is null) return Results.NotFound();
    if (a.Status != ApprovalStatus.Pending)
        return Results.BadRequest(new { error = $"Already decided: {a.Status}" });

    a.Status = body.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
    a.DecidedBy = body.DecidedBy ?? "ceo";
    a.Comment = body.Comment;
    a.DecidedAtUtc = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);

    await bus.Publish(new ApprovalDecidedEvent(
        a.Id, body.Approved, a.DecidedBy!, a.Comment, a.DecidedAtUtc.Value), ct);

    return Results.Ok(a);
});

app.Run();

public sealed record ApprovalDecision(bool Approved, string? DecidedBy, string? Comment);

using System.Text.Json;
using IWX.Common.Observability;
using IWX.Contracts.Automation;
using IWX.SchedulerEngine.Domain;
using IWX.SchedulerEngine.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.scheduler-engine");

builder.Services.AddDbContext<SchedulerDbContext>(o =>
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
        cfg.Message<SchedulerTickEvent>(m => m.SetEntityName(AutomationQueues.SchedulerTick));
    });
});

builder.Services.AddQuartz(q => { });
builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200", "http://localhost:8080")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// Bootstrap DB + reload persisted jobs into Quartz
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    var attempts = 0;
    while (attempts++ < 20)
    {
        try { db.Database.EnsureCreated(); break; } catch { Thread.Sleep(2000); }
    }
    var sched = await scope.ServiceProvider.GetRequiredService<ISchedulerFactory>().GetScheduler();
    foreach (var job in await db.Jobs.ToListAsync())
    {
        try { await JobRegistrar.RegisterAsync(sched, job, CancellationToken.None); } catch { /* skip bad cron */ }
    }
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok", engine = "scheduler" }));
app.MapGet("/engine", () => Results.Ok(EngineRegistry.SchedulerEngine));

app.MapGet("/jobs", async (SchedulerDbContext db) => Results.Ok(await db.Jobs.ToListAsync()));

app.MapGet("/jobs/{key}", async (string key, SchedulerDbContext db) =>
{
    var j = await db.Jobs.FirstOrDefaultAsync(x => x.Key == key);
    return j is null ? Results.NotFound() : Results.Ok(j);
});

app.MapPost("/jobs", async (
    ScheduledJob job, SchedulerDbContext db, ISchedulerFactory sf, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(job.Key) || string.IsNullOrWhiteSpace(job.CronExpression))
        return Results.BadRequest(new { error = "Key and CronExpression are required" });
    if (!CronExpression.IsValidExpression(job.CronExpression))
        return Results.BadRequest(new { error = $"Invalid cron expression: '{job.CronExpression}'" });

    var existing = await db.Jobs.FirstOrDefaultAsync(j => j.Key == job.Key, ct);
    if (existing is null)
    {
        job.CreatedAtUtc = DateTime.UtcNow;
        db.Jobs.Add(job);
    }
    else
    {
        existing.Name = job.Name;
        existing.CronExpression = job.CronExpression;
        existing.TargetDepartment = job.TargetDepartment;
        existing.PayloadJson = job.PayloadJson ?? "{}";
        existing.Enabled = job.Enabled;
        job = existing;
    }
    await db.SaveChangesAsync(ct);
    var sched = await sf.GetScheduler(ct);
    await JobRegistrar.RegisterAsync(sched, job, ct);
    return Results.Ok(job);
});

app.MapDelete("/jobs/{key}", async (
    string key, SchedulerDbContext db, ISchedulerFactory sf, CancellationToken ct) =>
{
    var j = await db.Jobs.FirstOrDefaultAsync(x => x.Key == key, ct);
    if (j is null) return Results.NotFound();
    db.Jobs.Remove(j);
    await db.SaveChangesAsync(ct);
    var sched = await sf.GetScheduler(ct);
    await JobRegistrar.UnregisterAsync(sched, key, ct);
    return Results.NoContent();
});

app.MapPost("/jobs/{key}/trigger", async (
    string key, SchedulerDbContext db, IPublishEndpoint bus, CancellationToken ct) =>
{
    var j = await db.Jobs.FirstOrDefaultAsync(x => x.Key == key, ct);
    if (j is null) return Results.NotFound();
    Dictionary<string, string> payload;
    try { payload = JsonSerializer.Deserialize<Dictionary<string, string>>(j.PayloadJson) ?? new(); }
    catch { payload = new(); }
    j.LastFiredAtUtc = DateTime.UtcNow;
    j.FireCount += 1;
    await db.SaveChangesAsync(ct);
    await bus.Publish(new SchedulerTickEvent(j.Key, j.Name, j.TargetDepartment, payload, DateTime.UtcNow), ct);
    return Results.Ok(j);
});

app.Run();

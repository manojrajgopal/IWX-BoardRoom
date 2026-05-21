using IWX.AuditService.Domain;
using IWX.AuditService.Infrastructure;
using IWX.Common.Observability;
using IWX.Contracts.Security;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.audit-service");

builder.Services.AddSingleton<AuditStore>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AuditRecordedConsumer>();
    x.AddConsumer<ThreatDetectedConsumer>();
    x.AddConsumer<AuthIssuedConsumer>();
    x.AddConsumer<AccessDeniedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var rmq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(rmq["Host"] ?? "localhost", h =>
        {
            h.Username(rmq["User"] ?? "guest");
            h.Password(rmq["Pass"] ?? "guest");
        });
        cfg.Message<AuditRecordedEvent>(m => m.SetEntityName(SecurityQueues.AuditRecorded));
        cfg.Message<ThreatDetectedEvent>(m => m.SetEntityName(SecurityQueues.ThreatDetected));
        cfg.Message<AuthIssuedEvent>(m => m.SetEntityName(SecurityQueues.AuthIssued));
        cfg.Message<AccessDeniedEvent>(m => m.SetEntityName(SecurityQueues.AccessDenied));

        cfg.ReceiveEndpoint("iwx.audit.recorded.q", e => e.ConfigureConsumer<AuditRecordedConsumer>(ctx));
        cfg.ReceiveEndpoint("iwx.audit.threat.q", e => e.ConfigureConsumer<ThreatDetectedConsumer>(ctx));
        cfg.ReceiveEndpoint("iwx.audit.auth.q", e => e.ConfigureConsumer<AuthIssuedConsumer>(ctx));
        cfg.ReceiveEndpoint("iwx.audit.denied.q", e => e.ConfigureConsumer<AccessDeniedConsumer>(ctx));
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200", "http://localhost:8080")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "audit" }));
app.MapGet("/service", () => Results.Ok(SecurityRegistry.AuditService));

app.MapPost("/audit", async (AuditRecord rec, AuditStore store, CancellationToken ct) =>
{
    rec.Source = string.IsNullOrEmpty(rec.Source) ? "api" : rec.Source;
    var saved = await store.AppendAsync(rec, ct);
    return Results.Ok(saved);
});

app.MapGet("/audit", async (string? actor, string? resource, string? action, int? take, AuditStore store, CancellationToken ct) =>
    Results.Ok(await store.QueryAsync(actor, resource, action, take ?? 100, ct)));

app.MapGet("/audit/verify", async (AuditStore store, CancellationToken ct) =>
{
    var (ok, n, failed) = await store.VerifyChainAsync(ct);
    return Results.Ok(new { ok, checkedCount = n, failedId = failed });
});

app.Run();

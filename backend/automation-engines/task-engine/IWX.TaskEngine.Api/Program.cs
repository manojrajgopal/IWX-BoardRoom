using IWX.Common.Observability;
using IWX.Contracts.Automation;
using IWX.TaskEngine.Domain;
using IWX.TaskEngine.Infrastructure;
using MassTransit;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.task-engine");

var mongoUri = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://iwx:iwx@mongo:27017";
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUri));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(
    EngineRegistry.TaskEngine.Database));

builder.Services.AddScoped<GraphRuntime>();

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
        cfg.Message<TaskNodeReadyEvent>(m => m.SetEntityName(AutomationQueues.TaskNodeReady));
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

app.MapGet("/health", () => Results.Ok(new { status = "ok", engine = "task" }));
app.MapGet("/engine", () => Results.Ok(EngineRegistry.TaskEngine));

app.MapPost("/graphs", async (TaskGraph g, GraphRuntime rt, CancellationToken ct) =>
    Results.Ok(await rt.CreateAsync(g, ct)));

app.MapGet("/graphs", async (int? limit, GraphRuntime rt, CancellationToken ct) =>
    Results.Ok(await rt.ListAsync(limit ?? 50, ct)));

app.MapGet("/graphs/{id:guid}", async (Guid id, GraphRuntime rt, CancellationToken ct) =>
{
    var g = await rt.GetAsync(id, ct);
    return g is null ? Results.NotFound() : Results.Ok(g);
});

app.MapPost("/graphs/{id:guid}/run", async (Guid id, GraphRuntime rt, CancellationToken ct) =>
    Results.Ok(await rt.RunAsync(id, ct)));

app.MapPost("/graphs/{id:guid}/nodes/{nodeId}/complete", async (
    Guid id, string nodeId, NodeResult body, GraphRuntime rt, CancellationToken ct) =>
    Results.Ok(await rt.CompleteNodeAsync(id, nodeId, body.Output, body.Failed, ct)));

app.Run();

public sealed record NodeResult(string? Output, bool Failed);

using IWX.Common.Observability;
using IWX.Contracts.Automation;
using IWX.WorkflowEngine.Domain;
using IWX.WorkflowEngine.Infrastructure;
using MassTransit;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.workflow-engine");

var mongoUri = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://iwx:iwx@mongo:27017";
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUri));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(
    EngineRegistry.WorkflowEngine.Database));

builder.Services.AddScoped<WorkflowRuntime>();

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
        cfg.Message<WorkflowStepDispatchedEvent>(m => m.SetEntityName(AutomationQueues.WorkflowStepDispatched));
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

app.MapGet("/health", () => Results.Ok(new { status = "ok", engine = "workflow" }));
app.MapGet("/engine", () => Results.Ok(EngineRegistry.WorkflowEngine));

app.MapPost("/workflows", async (WorkflowDefinition def, WorkflowRuntime rt, CancellationToken ct) =>
{
    await rt.UpsertDefinitionAsync(def, ct);
    return Results.Ok(def);
});

app.MapGet("/workflows", async (WorkflowRuntime rt, CancellationToken ct) =>
    Results.Ok(await rt.ListDefinitionsAsync(ct)));

app.MapGet("/workflows/{name}", async (string name, WorkflowRuntime rt, CancellationToken ct) =>
{
    var def = await rt.GetDefinitionAsync(name, ct);
    return def is null ? Results.NotFound() : Results.Ok(def);
});

app.MapPost("/workflows/{name}/start", async (
    string name, Dictionary<string, string>? context, WorkflowRuntime rt, CancellationToken ct) =>
    Results.Ok(await rt.StartAsync(name, context, ct)));

app.MapGet("/instances", async (int? limit, WorkflowRuntime rt, CancellationToken ct) =>
    Results.Ok(await rt.ListInstancesAsync(limit ?? 50, ct)));

app.MapGet("/instances/{id:guid}", async (Guid id, WorkflowRuntime rt, CancellationToken ct) =>
{
    var inst = await rt.GetInstanceAsync(id, ct);
    return inst is null ? Results.NotFound() : Results.Ok(inst);
});

app.MapPost("/instances/{id:guid}/signal", async (
    Guid id, SignalBody body, WorkflowRuntime rt, CancellationToken ct) =>
    Results.Ok(await rt.SignalAsync(id, body.StepId, body.Output, body.Failed, ct)));

app.Run();

public sealed record SignalBody(string StepId, string? Output, bool Failed);

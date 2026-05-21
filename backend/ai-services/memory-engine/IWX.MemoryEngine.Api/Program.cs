using IWX.Common.Observability;
using IWX.MemoryEngine.Domain;
using IWX.MemoryEngine.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.memory-engine");

builder.Services.AddSingleton<ILongTermMemoryStore, MongoLongTermMemoryStore>();
builder.Services.AddSingleton<IShortTermMemoryStore, RedisShortTermMemoryStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200", "http://localhost:8080")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "memory-engine" }));

// ---------- Long-term (Mongo) ----------
app.MapPut("/memory/long/{scope}/{key}", async (
    string scope, string key, MemoryUpsertRequest req,
    ILongTermMemoryStore store, CancellationToken ct) =>
{
    await store.UpsertAsync(scope, key, req.Value, req.Tags ?? Array.Empty<string>(), ct);
    return Results.Ok(new { scope, key });
});

app.MapGet("/memory/long/{scope}/{key}", async (
    string scope, string key, ILongTermMemoryStore store, CancellationToken ct) =>
{
    var entry = await store.GetAsync(scope, key, ct);
    return entry is null ? Results.NotFound() : Results.Ok(entry);
});

app.MapDelete("/memory/long/{scope}/{key}", async (
    string scope, string key, ILongTermMemoryStore store, CancellationToken ct) =>
{
    var ok = await store.DeleteAsync(scope, key, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/memory/long/{scope}", async (
    string scope, int? limit, ILongTermMemoryStore store, CancellationToken ct) =>
    Results.Ok(await store.ListAsync(scope, limit ?? 50, ct)));

app.MapGet("/memory/long/{scope}/search", async (
    string scope, string q, int? limit, ILongTermMemoryStore store, CancellationToken ct) =>
    Results.Ok(await store.SearchAsync(scope, q, limit ?? 20, ct)));

// ---------- Short-term (Redis) ----------
app.MapPut("/memory/short/{scope}/{key}", async (
    string scope, string key, MemoryUpsertRequest req, int? ttlSeconds,
    IShortTermMemoryStore store, CancellationToken ct) =>
{
    var ttl = ttlSeconds.HasValue ? TimeSpan.FromSeconds(ttlSeconds.Value) : (TimeSpan?)null;
    await store.SetAsync(scope, key, req.Value, ttl, ct);
    return Results.Ok(new { scope, key, ttlSeconds });
});

app.MapGet("/memory/short/{scope}/{key}", async (
    string scope, string key, IShortTermMemoryStore store, CancellationToken ct) =>
{
    var v = await store.GetAsync(scope, key, ct);
    return v is null ? Results.NotFound() : Results.Ok(new { scope, key, value = v });
});

app.MapDelete("/memory/short/{scope}/{key}", async (
    string scope, string key, IShortTermMemoryStore store, CancellationToken ct) =>
{
    var ok = await store.DeleteAsync(scope, key, ct);
    return ok ? Results.NoContent() : Results.NotFound();
});

app.Run();

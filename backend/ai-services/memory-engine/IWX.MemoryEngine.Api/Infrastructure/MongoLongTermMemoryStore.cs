using IWX.MemoryEngine.Domain;
using MongoDB.Driver;

namespace IWX.MemoryEngine.Infrastructure;

public interface ILongTermMemoryStore
{
    Task UpsertAsync(string scope, string key, string value, string[] tags, CancellationToken ct);
    Task<MemoryEntry?> GetAsync(string scope, string key, CancellationToken ct);
    Task<bool> DeleteAsync(string scope, string key, CancellationToken ct);
    Task<IReadOnlyList<MemorySearchHit>> SearchAsync(string scope, string query, int limit, CancellationToken ct);
    Task<IReadOnlyList<MemorySearchHit>> ListAsync(string scope, int limit, CancellationToken ct);
}

public sealed class MongoLongTermMemoryStore : ILongTermMemoryStore
{
    private readonly IMongoCollection<MemoryEntry> _col;

    public MongoLongTermMemoryStore(IConfiguration cfg)
    {
        var conn = cfg.GetConnectionString("Mongo") ?? "mongodb://iwx:iwx@localhost:27017";
        var client = new MongoClient(conn);
        var db = client.GetDatabase(cfg["Mongo:Database"] ?? "iwx_memory");
        _col = db.GetCollection<MemoryEntry>("entries");

        _col.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<MemoryEntry>(
                Builders<MemoryEntry>.IndexKeys.Ascending(x => x.Scope).Ascending(x => x.Key),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<MemoryEntry>(
                Builders<MemoryEntry>.IndexKeys.Text(x => x.Value).Text(x => x.Key))
        });
    }

    public async Task UpsertAsync(string scope, string key, string value, string[] tags, CancellationToken ct)
    {
        var filter = Builders<MemoryEntry>.Filter.And(
            Builders<MemoryEntry>.Filter.Eq(x => x.Scope, scope),
            Builders<MemoryEntry>.Filter.Eq(x => x.Key, key));

        var update = Builders<MemoryEntry>.Update
            .Set(x => x.Value, value)
            .Set(x => x.Tags, tags)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow)
            .SetOnInsert(x => x.Scope, scope)
            .SetOnInsert(x => x.Key, key)
            .SetOnInsert(x => x.CreatedAtUtc, DateTime.UtcNow);

        await _col.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
    }

    public Task<MemoryEntry?> GetAsync(string scope, string key, CancellationToken ct) =>
        _col.Find(x => x.Scope == scope && x.Key == key).FirstOrDefaultAsync(ct)!;

    public async Task<bool> DeleteAsync(string scope, string key, CancellationToken ct)
    {
        var r = await _col.DeleteOneAsync(x => x.Scope == scope && x.Key == key, ct);
        return r.DeletedCount > 0;
    }

    public async Task<IReadOnlyList<MemorySearchHit>> SearchAsync(string scope, string query, int limit, CancellationToken ct)
    {
        var filter = Builders<MemoryEntry>.Filter.And(
            Builders<MemoryEntry>.Filter.Eq(x => x.Scope, scope),
            Builders<MemoryEntry>.Filter.Text(query));

        var hits = await _col.Find(filter).Limit(limit).ToListAsync(ct);
        return hits.Select(h => new MemorySearchHit(h.Scope, h.Key, h.Value, h.Tags, h.UpdatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<MemorySearchHit>> ListAsync(string scope, int limit, CancellationToken ct)
    {
        var hits = await _col.Find(x => x.Scope == scope)
            .SortByDescending(x => x.UpdatedAtUtc)
            .Limit(limit)
            .ToListAsync(ct);
        return hits.Select(h => new MemorySearchHit(h.Scope, h.Key, h.Value, h.Tags, h.UpdatedAtUtc)).ToList();
    }
}

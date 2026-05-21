using StackExchange.Redis;

namespace IWX.MemoryEngine.Infrastructure;

public interface IShortTermMemoryStore
{
    Task SetAsync(string scope, string key, string value, TimeSpan? ttl, CancellationToken ct);
    Task<string?> GetAsync(string scope, string key, CancellationToken ct);
    Task<bool> DeleteAsync(string scope, string key, CancellationToken ct);
}

public sealed class RedisShortTermMemoryStore : IShortTermMemoryStore
{
    private readonly IDatabase _db;
    private const string Prefix = "iwx:mem";

    public RedisShortTermMemoryStore(IConfiguration cfg)
    {
        var conn = cfg.GetConnectionString("Redis") ?? "localhost:6379";
        var mux = ConnectionMultiplexer.Connect(conn);
        _db = mux.GetDatabase();
    }

    private static string K(string scope, string key) => $"{Prefix}:{scope}:{key}";

    public async Task SetAsync(string scope, string key, string value, TimeSpan? ttl, CancellationToken ct)
    {
        await _db.StringSetAsync(K(scope, key), value, ttl);
    }

    public async Task<string?> GetAsync(string scope, string key, CancellationToken ct)
    {
        var v = await _db.StringGetAsync(K(scope, key));
        return v.HasValue ? v.ToString() : null;
    }

    public async Task<bool> DeleteAsync(string scope, string key, CancellationToken ct) =>
        await _db.KeyDeleteAsync(K(scope, key));
}

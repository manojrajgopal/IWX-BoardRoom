using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using IWX.AuditService.Domain;
using MongoDB.Driver;

namespace IWX.AuditService.Infrastructure;

public sealed class AuditStore
{
    private readonly IMongoCollection<AuditRecord> _col;
    private readonly IProducer<string, string>? _kafka;
    private readonly string _kafkaTopic;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AuditStore(IConfiguration cfg)
    {
        var client = new MongoClient(cfg["Mongo:ConnectionString"]);
        var db = client.GetDatabase(cfg["Mongo:Database"] ?? "iwx_audit");
        _col = db.GetCollection<AuditRecord>("records");

        var indexBuilder = Builders<AuditRecord>.IndexKeys;
        _col.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<AuditRecord>(indexBuilder.Descending(r => r.RecordedAtUtc)),
            new CreateIndexModel<AuditRecord>(indexBuilder.Ascending(r => r.Actor)),
            new CreateIndexModel<AuditRecord>(indexBuilder.Ascending(r => r.Resource)),
            new CreateIndexModel<AuditRecord>(indexBuilder.Ascending(r => r.Action)),
        });

        var bootstrap = cfg["Kafka:BootstrapServers"];
        _kafkaTopic = cfg["Kafka:Topic"] ?? "iwx.audit";
        if (!string.IsNullOrWhiteSpace(bootstrap))
        {
            var pcfg = new ProducerConfig { BootstrapServers = bootstrap, Acks = Acks.All };
            try { _kafka = new ProducerBuilder<string, string>(pcfg).Build(); }
            catch { _kafka = null; }
        }
    }

    public async Task<AuditRecord> AppendAsync(AuditRecord rec, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var last = await _col.Find(FilterDefinition<AuditRecord>.Empty)
                .Sort(Builders<AuditRecord>.Sort.Descending(r => r.RecordedAtUtc))
                .Limit(1).FirstOrDefaultAsync(ct);

            rec.PrevHash = last?.Hash ?? "GENESIS";
            rec.RecordedAtUtc = rec.RecordedAtUtc == default ? DateTime.UtcNow : rec.RecordedAtUtc;
            var canonical = JsonSerializer.Serialize(new
            {
                rec.Id, rec.Actor, rec.Action, rec.Resource, rec.Outcome,
                rec.PayloadJson, rec.Source, rec.RecordedAtUtc, rec.PrevHash
            });
            rec.Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

            await _col.InsertOneAsync(rec, cancellationToken: ct);

            if (_kafka is not null)
            {
                try
                {
                    await _kafka.ProduceAsync(_kafkaTopic, new Message<string, string>
                    {
                        Key = rec.Resource,
                        Value = JsonSerializer.Serialize(rec)
                    }, ct);
                }
                catch { /* kafka best-effort */ }
            }

            return rec;
        }
        finally { _lock.Release(); }
    }

    public Task<List<AuditRecord>> QueryAsync(string? actor, string? resource, string? action, int take, CancellationToken ct)
    {
        var fb = Builders<AuditRecord>.Filter;
        var f = FilterDefinition<AuditRecord>.Empty;
        if (!string.IsNullOrEmpty(actor)) f &= fb.Eq(r => r.Actor, actor);
        if (!string.IsNullOrEmpty(resource)) f &= fb.Eq(r => r.Resource, resource);
        if (!string.IsNullOrEmpty(action)) f &= fb.Eq(r => r.Action, action);

        return _col.Find(f)
            .Sort(Builders<AuditRecord>.Sort.Descending(r => r.RecordedAtUtc))
            .Limit(Math.Clamp(take, 1, 1000))
            .ToListAsync(ct);
    }

    public async Task<(bool ok, int checkedCount, string? failedId)> VerifyChainAsync(CancellationToken ct)
    {
        var all = await _col.Find(FilterDefinition<AuditRecord>.Empty)
            .Sort(Builders<AuditRecord>.Sort.Ascending(r => r.RecordedAtUtc))
            .ToListAsync(ct);

        var prev = "GENESIS";
        foreach (var rec in all)
        {
            if (rec.PrevHash != prev) return (false, all.Count, rec.Id.ToString());
            var canonical = JsonSerializer.Serialize(new
            {
                rec.Id, rec.Actor, rec.Action, rec.Resource, rec.Outcome,
                rec.PayloadJson, rec.Source, rec.RecordedAtUtc, rec.PrevHash
            });
            var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
            if (expected != rec.Hash) return (false, all.Count, rec.Id.ToString());
            prev = rec.Hash;
        }
        return (true, all.Count, null);
    }
}

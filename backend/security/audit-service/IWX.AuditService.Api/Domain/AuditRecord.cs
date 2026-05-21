using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IWX.AuditService.Domain;

public sealed class AuditRecord
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("actor")]
    public string Actor { get; set; } = "";

    [BsonElement("action")]
    public string Action { get; set; } = "";

    [BsonElement("resource")]
    public string Resource { get; set; } = "";

    [BsonElement("outcome")]
    public string? Outcome { get; set; }

    [BsonElement("payload")]
    public string? PayloadJson { get; set; }

    [BsonElement("source")]
    public string Source { get; set; } = "api";

    [BsonElement("recordedAtUtc")]
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Append-only hash chain: SHA256(prev.Hash + this.canonical).</summary>
    [BsonElement("hash")]
    public string Hash { get; set; } = "";

    [BsonElement("prevHash")]
    public string PrevHash { get; set; } = "";
}

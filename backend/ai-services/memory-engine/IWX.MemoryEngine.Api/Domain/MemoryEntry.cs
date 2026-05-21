using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IWX.MemoryEngine.Domain;

public sealed class MemoryEntry
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Scope { get; set; } = string.Empty;   // e.g. "hr", "ceo", "global"
    public string Key { get; set; } = string.Empty;     // e.g. "task:guid", "fact:onboarding-policy"
    public string Value { get; set; } = string.Empty;   // free text or JSON
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed record MemoryUpsertRequest(string Value, string[]? Tags);

public sealed record MemorySearchHit(string Scope, string Key, string Value, string[] Tags, DateTime UpdatedAtUtc);

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IWX.Connectors.Worker.Infrastructure;

/// <summary>
/// Opaque credential bag stored in Mongo per connector account.
/// Encryption-at-rest will be added by the auth-service in Phase 6.
/// </summary>
public sealed class ConnectorCredential
{
    [BsonId]
    public string Account { get; set; } = "default";
    public Dictionary<string, string> Values { get; set; } = new();
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

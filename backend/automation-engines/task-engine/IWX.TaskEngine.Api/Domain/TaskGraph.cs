using MongoDB.Bson.Serialization.Attributes;

namespace IWX.TaskEngine.Domain;

public enum NodeStatus { Pending, Ready, Running, Completed, Failed, Skipped }

public sealed class GraphNode
{
    public string Id { get; set; } = "";
    public string TargetDepartment { get; set; } = "";
    public string Action { get; set; } = "";
    public Dictionary<string, string> Input { get; set; } = new();
    public List<string> DependsOn { get; set; } = new();
    public NodeStatus Status { get; set; } = NodeStatus.Pending;
    public string? Output { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class TaskGraph
{
    [BsonId] public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Status { get; set; } = "draft"; // draft | running | completed | failed
    public List<GraphNode> Nodes { get; set; } = new();
    public Dictionary<string, string> Context { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

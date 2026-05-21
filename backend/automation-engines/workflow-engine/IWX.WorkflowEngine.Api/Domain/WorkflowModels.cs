using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IWX.WorkflowEngine.Domain;

public sealed class WorkflowStep
{
    public string Id { get; set; } = "";
    public string TargetDepartment { get; set; } = "";
    public string Action { get; set; } = "";
    public Dictionary<string, string> Input { get; set; } = new();
    public List<string> DependsOn { get; set; } = new();
}

public sealed class WorkflowDefinition
{
    [BsonId] public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<WorkflowStep> Steps { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum StepStatus { Pending, Dispatched, Completed, Failed, Skipped }

public sealed class StepState
{
    public string StepId { get; set; } = "";
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Output { get; set; }
}

public sealed class WorkflowInstance
{
    [BsonId] public Guid Id { get; set; } = Guid.NewGuid();
    public string WorkflowName { get; set; } = "";
    public string Status { get; set; } = "running"; // running | completed | failed
    public List<StepState> Steps { get; set; } = new();
    public Dictionary<string, string> Context { get; set; } = new();
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

namespace IWX.Contracts.Automation;

/// <summary>
/// Stable keys for every automation engine in the IWX Boardroom.
/// Used for routing, queue names, and docker hostnames.
/// </summary>
public static class Engines
{
    public const string Workflow = "workflow";
    public const string Scheduler = "scheduler";
    public const string Task = "task";
    public const string Approval = "approval";
}

public sealed record EngineDescriptor(
    string Key,
    string DisplayName,
    string Database,
    int HttpPort,
    string ServiceHost,
    string Icon);

public static class EngineRegistry
{
    public static readonly EngineDescriptor WorkflowEngine = new(
        Engines.Workflow, "Workflow Engine", "iwx_workflows", 8300, "workflow-engine", "pi-sitemap");

    public static readonly EngineDescriptor SchedulerEngine = new(
        Engines.Scheduler, "Scheduler Engine", "IwxScheduler", 8301, "scheduler-engine", "pi-clock");

    public static readonly EngineDescriptor TaskEngine = new(
        Engines.Task, "Task Engine", "iwx_tasks", 8302, "task-engine", "pi-list");

    public static readonly EngineDescriptor ApprovalEngine = new(
        Engines.Approval, "Approval Engine", "IwxApprovals", 8303, "approval-engine", "pi-check-square");

    public static readonly IReadOnlyList<EngineDescriptor> All = new[]
    {
        WorkflowEngine, SchedulerEngine, TaskEngine, ApprovalEngine
    };
}

public static class AutomationQueues
{
    /// <summary>A workflow step has been dispatched (consumed by task-engine or department services).</summary>
    public const string WorkflowStepDispatched = "iwx.workflow.step.dispatched";
    /// <summary>A scheduled job has fired its cron trigger.</summary>
    public const string SchedulerTick = "iwx.scheduler.tick";
    /// <summary>A task graph node is ready to run.</summary>
    public const string TaskNodeReady = "iwx.task.node.ready";
    /// <summary>A CEO approval has been requested for a pending task.</summary>
    public const string ApprovalRequested = "iwx.approval.requested";
    /// <summary>The CEO has approved or rejected a pending task.</summary>
    public const string ApprovalDecided = "iwx.approval.decided";
}

// ---- Event payloads ----

public sealed record WorkflowStepDispatchedEvent(
    Guid WorkflowInstanceId,
    string WorkflowName,
    string StepId,
    string TargetDepartment,
    string Action,
    Dictionary<string, string> Input,
    DateTime DispatchedAtUtc);

public sealed record SchedulerTickEvent(
    string JobKey,
    string JobName,
    string TargetDepartment,
    Dictionary<string, string> Payload,
    DateTime FiredAtUtc);

public sealed record TaskNodeReadyEvent(
    Guid GraphId,
    string NodeId,
    string TargetDepartment,
    string Action,
    Dictionary<string, string> Input,
    DateTime ReadyAtUtc);

public sealed record ApprovalRequestedEvent(
    Guid ApprovalId,
    string Subject,
    string Requester,
    string TargetDepartment,
    string Priority,
    string PayloadJson,
    DateTime RequestedAtUtc);

public sealed record ApprovalDecidedEvent(
    Guid ApprovalId,
    bool Approved,
    string DecidedBy,
    string? Comment,
    DateTime DecidedAtUtc);

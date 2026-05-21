namespace IWX.Contracts.Events;

public sealed record TaskApprovedEvent(
    Guid TaskId,
    string Title,
    string Description,
    string TargetDepartment,
    string Priority,
    DateTime ApprovedAtUtc,
    string ApprovedBy);

public sealed record TaskCompletedEvent(
    Guid TaskId,
    string TargetDepartment,
    string ResultSummary,
    string ResultPayloadJson,
    DateTime CompletedAtUtc);

public sealed record AgentThinkingEvent(
    Guid TaskId,
    string Department,
    string Stage,
    string Message,
    DateTime TimestampUtc);

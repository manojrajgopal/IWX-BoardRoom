using IWX.Contracts.Events;

namespace IWX.Departments.Worker.Brain;

/// <summary>
/// Department-specific cognition. Phase 2 ships a default brain that emits
/// structured thinking events and produces a placeholder completion summary.
/// Later phases will plug in real LLM/RAG/CrewAI brains per department.
/// </summary>
public interface IDepartmentBrain
{
    Task<DepartmentBrainResult> ProcessAsync(
        TaskApprovedEvent task,
        IDepartmentThinkingChannel thinking,
        CancellationToken ct);
}

public sealed record DepartmentBrainResult(string Summary, string PayloadJson);

public interface IDepartmentThinkingChannel
{
    Task EmitAsync(string stage, string message, CancellationToken ct);
}

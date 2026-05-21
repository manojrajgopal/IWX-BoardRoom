using IWX.Contracts.Departments;
using IWX.Contracts.Events;

namespace IWX.Departments.Worker.Brain;

/// <summary>
/// Phase 2 placeholder brain: deterministic, fast, no external LLM calls.
/// Emits structured thinking events so the UI lights up with realtime activity.
/// </summary>
public sealed class DefaultDepartmentBrain(DepartmentDescriptor department) : IDepartmentBrain
{
    public async Task<DepartmentBrainResult> ProcessAsync(
        TaskApprovedEvent task,
        IDepartmentThinkingChannel thinking,
        CancellationToken ct)
    {
        await thinking.EmitAsync("received",
            $"{department.DisplayName} received task '{task.Title}'", ct);

        await Task.Delay(400, ct);
        await thinking.EmitAsync("planning",
            $"{department.DisplayName} is decomposing the request into actionable steps.", ct);

        await Task.Delay(600, ct);
        await thinking.EmitAsync("executing",
            $"{department.DisplayName} is executing its standard playbook for priority={task.Priority}.", ct);

        await Task.Delay(400, ct);
        await thinking.EmitAsync("completed",
            $"{department.DisplayName} finished processing '{task.Title}'.", ct);

        var summary = $"[{department.DisplayName}] Processed task '{task.Title}' via default playbook.";
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            department = department.Key,
            taskId = task.TaskId,
            steps = new[] { "received", "planning", "executing", "completed" },
            notes = "Phase 2 default brain — replace with specialized brain in later phases."
        });

        return new DepartmentBrainResult(summary, payload);
    }
}

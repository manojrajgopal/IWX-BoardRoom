using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IWX.Contracts.Departments;
using IWX.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace IWX.Departments.Worker.Brain;

/// <summary>
/// Phase 3 brain that delegates to the AI substrate:
///   1) reasoning-engine — multi-agent crew produces a plan + final response
///   2) memory-engine    — persists the result as long-term memory for the dept
/// Emits thinking events at each substrate hop so the dashboard lights up.
/// </summary>
public sealed class SubstrateDepartmentBrain(
    DepartmentDescriptor department,
    HttpClient httpClient,
    SubstrateOptions options,
    ILogger<SubstrateDepartmentBrain> log) : IDepartmentBrain
{
    public async Task<DepartmentBrainResult> ProcessAsync(
        TaskApprovedEvent task,
        IDepartmentThinkingChannel thinking,
        CancellationToken ct)
    {
        await thinking.EmitAsync("received",
            $"{department.DisplayName} received task '{task.Title}'", ct);

        await thinking.EmitAsync("reasoning",
            $"{department.DisplayName} is dispatching the task to the reasoning-engine crew.", ct);

        string finalText;
        string transcriptJson;
        try
        {
            using var resp = await httpClient.PostAsJsonAsync(
                $"{options.ReasoningEngineUrl.TrimEnd('/')}/reason",
                new
                {
                    department = department.Key,
                    task_title = task.Title,
                    task_description = task.Description,
                    extra_context = new { priority = task.Priority, approvedBy = task.ApprovedBy }
                },
                ct);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            finalText = doc.RootElement.GetProperty("final").GetString() ?? string.Empty;
            transcriptJson = doc.RootElement.GetRawText();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "{Dept} reasoning-engine unreachable; falling back to default brain.", department.Key);
            await thinking.EmitAsync("fallback",
                $"{department.DisplayName} could not reach reasoning-engine; using default playbook.", ct);
            var fallback = await new DefaultDepartmentBrain(department).ProcessAsync(task, thinking, ct);
            return fallback;
        }

        await thinking.EmitAsync("memorizing",
            $"{department.DisplayName} is committing the outcome to long-term memory.", ct);

        try
        {
            using var put = await httpClient.PutAsync(
                $"{options.MemoryEngineUrl.TrimEnd('/')}/memory/long/{department.Key}/task:{task.TaskId}",
                new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        value = finalText,
                        tags = new[] { "task", task.Priority.ToLowerInvariant(), department.Key }
                    }),
                    Encoding.UTF8,
                    "application/json"),
                ct);
            if (!put.IsSuccessStatusCode)
            {
                log.LogWarning("{Dept} memory-engine write failed: {Status}", department.Key, put.StatusCode);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "{Dept} memory-engine unreachable.", department.Key);
        }

        await thinking.EmitAsync("completed",
            $"{department.DisplayName} finished processing '{task.Title}'.", ct);

        var summary = finalText.Length > 280 ? finalText[..280] + "…" : finalText;
        return new DepartmentBrainResult(summary, transcriptJson);
    }
}

public sealed class SubstrateOptions
{
    public string ReasoningEngineUrl { get; set; } = "http://reasoning-engine:8105";
    public string MemoryEngineUrl { get; set; } = "http://memory-engine:8100";
}

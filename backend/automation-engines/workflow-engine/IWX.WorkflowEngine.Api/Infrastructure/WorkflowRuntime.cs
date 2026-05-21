using IWX.Contracts.Automation;
using IWX.WorkflowEngine.Domain;
using MassTransit;
using MongoDB.Driver;

namespace IWX.WorkflowEngine.Infrastructure;

public sealed class WorkflowRuntime
{
    private readonly IMongoCollection<WorkflowDefinition> _defs;
    private readonly IMongoCollection<WorkflowInstance> _instances;
    private readonly IPublishEndpoint _bus;

    public WorkflowRuntime(IMongoDatabase db, IPublishEndpoint bus)
    {
        _defs = db.GetCollection<WorkflowDefinition>("definitions");
        _instances = db.GetCollection<WorkflowInstance>("instances");
        _bus = bus;
    }

    public async Task UpsertDefinitionAsync(WorkflowDefinition def, CancellationToken ct)
    {
        def.CreatedAtUtc = DateTime.UtcNow;
        await _defs.ReplaceOneAsync(d => d.Name == def.Name, def, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public Task<WorkflowDefinition?> GetDefinitionAsync(string name, CancellationToken ct) =>
        _defs.Find(d => d.Name == name).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<WorkflowDefinition>> ListDefinitionsAsync(CancellationToken ct) =>
        await _defs.Find(FilterDefinition<WorkflowDefinition>.Empty).ToListAsync(ct);

    public Task<WorkflowInstance?> GetInstanceAsync(Guid id, CancellationToken ct) =>
        _instances.Find(i => i.Id == id).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<WorkflowInstance>> ListInstancesAsync(int limit, CancellationToken ct) =>
        await _instances.Find(FilterDefinition<WorkflowInstance>.Empty)
            .SortByDescending(i => i.StartedAtUtc).Limit(limit).ToListAsync(ct);

    public async Task<WorkflowInstance> StartAsync(string name, Dictionary<string, string>? context, CancellationToken ct)
    {
        var def = await GetDefinitionAsync(name, ct)
            ?? throw new InvalidOperationException($"Workflow '{name}' not found");

        var inst = new WorkflowInstance
        {
            WorkflowName = name,
            Context = context ?? new(),
            Steps = def.Steps.Select(s => new StepState { StepId = s.Id }).ToList()
        };
        await _instances.InsertOneAsync(inst, cancellationToken: ct);
        await DispatchReadyStepsAsync(inst, def, ct);
        return inst;
    }

    public async Task<WorkflowInstance> SignalAsync(Guid id, string stepId, string? output, bool failed, CancellationToken ct)
    {
        var inst = await GetInstanceAsync(id, ct)
            ?? throw new InvalidOperationException($"Instance {id} not found");
        var step = inst.Steps.FirstOrDefault(s => s.StepId == stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not in instance");

        step.Status = failed ? StepStatus.Failed : StepStatus.Completed;
        step.CompletedAtUtc = DateTime.UtcNow;
        step.Output = output;

        var def = await GetDefinitionAsync(inst.WorkflowName, ct)
            ?? throw new InvalidOperationException("Definition disappeared");

        if (failed)
        {
            inst.Status = "failed";
            inst.CompletedAtUtc = DateTime.UtcNow;
        }
        else if (inst.Steps.All(s => s.Status == StepStatus.Completed || s.Status == StepStatus.Skipped))
        {
            inst.Status = "completed";
            inst.CompletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            await DispatchReadyStepsAsync(inst, def, ct);
        }
        await _instances.ReplaceOneAsync(i => i.Id == inst.Id, inst, cancellationToken: ct);
        return inst;
    }

    private async Task DispatchReadyStepsAsync(WorkflowInstance inst, WorkflowDefinition def, CancellationToken ct)
    {
        foreach (var state in inst.Steps.Where(s => s.Status == StepStatus.Pending))
        {
            var step = def.Steps.First(s => s.Id == state.StepId);
            var allDepsDone = step.DependsOn.All(d =>
                inst.Steps.Any(s => s.StepId == d && s.Status == StepStatus.Completed));
            if (!allDepsDone) continue;

            state.Status = StepStatus.Dispatched;
            state.DispatchedAtUtc = DateTime.UtcNow;
            await _bus.Publish(new WorkflowStepDispatchedEvent(
                inst.Id, inst.WorkflowName, step.Id, step.TargetDepartment, step.Action,
                step.Input, DateTime.UtcNow), ct);
        }
        await _instances.ReplaceOneAsync(i => i.Id == inst.Id, inst, cancellationToken: ct);
    }
}

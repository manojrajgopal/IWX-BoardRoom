using IWX.Contracts.Automation;
using IWX.TaskEngine.Domain;
using MassTransit;
using MongoDB.Driver;

namespace IWX.TaskEngine.Infrastructure;

public sealed class GraphRuntime
{
    private readonly IMongoCollection<TaskGraph> _graphs;
    private readonly IPublishEndpoint _bus;

    public GraphRuntime(IMongoDatabase db, IPublishEndpoint bus)
    {
        _graphs = db.GetCollection<TaskGraph>("graphs");
        _bus = bus;
    }

    public async Task<TaskGraph> CreateAsync(TaskGraph graph, CancellationToken ct)
    {
        graph.CreatedAtUtc = DateTime.UtcNow;
        graph.Status = "draft";
        await _graphs.InsertOneAsync(graph, cancellationToken: ct);
        return graph;
    }

    public Task<TaskGraph?> GetAsync(Guid id, CancellationToken ct) =>
        _graphs.Find(g => g.Id == id).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<TaskGraph>> ListAsync(int limit, CancellationToken ct) =>
        await _graphs.Find(FilterDefinition<TaskGraph>.Empty)
            .SortByDescending(g => g.CreatedAtUtc).Limit(limit).ToListAsync(ct);

    public async Task<TaskGraph> RunAsync(Guid id, CancellationToken ct)
    {
        var g = await GetAsync(id, ct) ?? throw new InvalidOperationException("Graph not found");
        if (g.Status == "running" || g.Status == "completed") return g;
        g.Status = "running";
        g.StartedAtUtc = DateTime.UtcNow;
        await DispatchReadyAsync(g, ct);
        return g;
    }

    public async Task<TaskGraph> CompleteNodeAsync(
        Guid id, string nodeId, string? output, bool failed, CancellationToken ct)
    {
        var g = await GetAsync(id, ct) ?? throw new InvalidOperationException("Graph not found");
        var n = g.Nodes.FirstOrDefault(x => x.Id == nodeId)
            ?? throw new InvalidOperationException($"Node '{nodeId}' not in graph");

        n.Status = failed ? NodeStatus.Failed : NodeStatus.Completed;
        n.Output = output;
        n.CompletedAtUtc = DateTime.UtcNow;

        if (failed) { g.Status = "failed"; g.CompletedAtUtc = DateTime.UtcNow; }
        else if (g.Nodes.All(x => x.Status is NodeStatus.Completed or NodeStatus.Skipped))
        {
            g.Status = "completed"; g.CompletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            await DispatchReadyAsync(g, ct);
        }
        await _graphs.ReplaceOneAsync(x => x.Id == g.Id, g, cancellationToken: ct);
        return g;
    }

    private async Task DispatchReadyAsync(TaskGraph g, CancellationToken ct)
    {
        foreach (var n in g.Nodes.Where(n => n.Status == NodeStatus.Pending))
        {
            var ready = n.DependsOn.All(d =>
                g.Nodes.Any(x => x.Id == d && x.Status == NodeStatus.Completed));
            if (!ready) continue;

            n.Status = NodeStatus.Ready;
            n.StartedAtUtc = DateTime.UtcNow;
            await _bus.Publish(new TaskNodeReadyEvent(
                g.Id, n.Id, n.TargetDepartment, n.Action, n.Input, DateTime.UtcNow), ct);
        }
        await _graphs.ReplaceOneAsync(x => x.Id == g.Id, g, cancellationToken: ct);
    }
}

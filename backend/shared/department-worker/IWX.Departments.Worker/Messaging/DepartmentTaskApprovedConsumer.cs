using IWX.Contracts.Departments;
using IWX.Contracts.Events;
using IWX.Departments.Worker.Brain;
using IWX.Departments.Worker.Domain;
using IWX.Departments.Worker.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IWX.Departments.Worker.Messaging;

/// <summary>
/// Bound to the <c>iwx.task.approved</c> fanout. Every department service
/// receives every approved task; this consumer filters by the descriptor's
/// key so each service only acts on tasks routed to it.
/// </summary>
public sealed class DepartmentTaskApprovedConsumer(
    DepartmentDescriptor department,
    DepartmentDbContext db,
    IDepartmentBrain brain,
    IPublishEndpoint publisher,
    ILogger<DepartmentTaskApprovedConsumer> log) : IConsumer<TaskApprovedEvent>
{
    public async Task Consume(ConsumeContext<TaskApprovedEvent> context)
    {
        var evt = context.Message;
        if (!string.Equals(evt.TargetDepartment, department.Key, StringComparison.OrdinalIgnoreCase))
        {
            return; // not for me
        }

        log.LogInformation("{Dept} picked up task {TaskId} '{Title}'",
            department.Key, evt.TaskId, evt.Title);

        var task = new DepartmentTask
        {
            Id = evt.TaskId,
            Title = evt.Title,
            Description = evt.Description,
            Priority = evt.Priority,
            ApprovedBy = evt.ApprovedBy,
            ReceivedAtUtc = DateTime.UtcNow,
            Status = DepartmentTaskStatus.Received
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync(context.CancellationToken);

        var channel = new PublisherThinkingChannel(department, evt.TaskId, publisher);

        try
        {
            task.Status = DepartmentTaskStatus.Processing;
            await db.SaveChangesAsync(context.CancellationToken);

            var result = await brain.ProcessAsync(evt, channel, context.CancellationToken);

            task.Status = DepartmentTaskStatus.Completed;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.ResultSummary = result.Summary;
            task.ResultPayloadJson = result.PayloadJson;
            await db.SaveChangesAsync(context.CancellationToken);

            await publisher.Publish(new TaskCompletedEvent(
                evt.TaskId,
                department.Key,
                result.Summary,
                result.PayloadJson,
                DateTime.UtcNow), context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "{Dept} failed task {TaskId}", department.Key, evt.TaskId);
            task.Status = DepartmentTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(context.CancellationToken);

            await publisher.Publish(new TaskCompletedEvent(
                evt.TaskId,
                department.Key,
                $"[FAILED] {ex.Message}",
                "{}",
                DateTime.UtcNow), context.CancellationToken);
        }
    }
}

internal sealed class PublisherThinkingChannel(
    DepartmentDescriptor department,
    Guid taskId,
    IPublishEndpoint publisher) : IDepartmentThinkingChannel
{
    public Task EmitAsync(string stage, string message, CancellationToken ct) =>
        publisher.Publish(new AgentThinkingEvent(
            taskId, department.Key, stage, message, DateTime.UtcNow), ct);
}

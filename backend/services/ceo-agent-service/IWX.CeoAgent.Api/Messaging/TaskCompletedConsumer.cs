using IWX.CeoAgent.Domain;
using IWX.CeoAgent.Infrastructure;
using IWX.CeoAgent.Realtime;
using IWX.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IWX.CeoAgent.Messaging;

public sealed class TaskCompletedConsumer(
    CeoDbContext db,
    IHubContext<BoardroomHub> hub,
    ILogger<TaskCompletedConsumer> log) : IConsumer<TaskCompletedEvent>
{
    public async Task Consume(ConsumeContext<TaskCompletedEvent> context)
    {
        var evt = context.Message;
        var task = await db.BoardTasks.FirstOrDefaultAsync(x => x.Id == evt.TaskId);
        if (task is not null)
        {
            task.Status = BoardTaskStatus.Completed;
            task.CompletedAtUtc = evt.CompletedAtUtc;
            task.ResultSummary = evt.ResultSummary;
            task.ResultPayloadJson = evt.ResultPayloadJson;
            await db.SaveChangesAsync();
        }
        log.LogInformation("Task {TaskId} completed by {Dept}", evt.TaskId, evt.TargetDepartment);
        await hub.Clients.All.SendAsync("taskCompleted", evt);
    }
}

public sealed class AgentThinkingConsumer(
    IHubContext<BoardroomHub> hub) : IConsumer<AgentThinkingEvent>
{
    public async Task Consume(ConsumeContext<AgentThinkingEvent> context)
    {
        await hub.Clients.All.SendAsync("agentThinking", context.Message);
    }
}

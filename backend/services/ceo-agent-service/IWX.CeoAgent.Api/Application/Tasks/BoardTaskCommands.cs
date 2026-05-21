using IWX.CeoAgent.Domain;
using IWX.CeoAgent.Infrastructure;
using IWX.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IWX.CeoAgent.Application.Tasks;

public sealed record CreateBoardTaskCommand(
    string Title,
    string Description,
    string TargetDepartment,
    string Priority) : IRequest<Guid>;

public sealed class CreateBoardTaskHandler(CeoDbContext db) : IRequestHandler<CreateBoardTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateBoardTaskCommand request, CancellationToken ct)
    {
        var task = new BoardTask
        {
            Title = request.Title,
            Description = request.Description,
            TargetDepartment = request.TargetDepartment,
            Priority = request.Priority,
            Status = BoardTaskStatus.PendingApproval
        };
        db.BoardTasks.Add(task);
        await db.SaveChangesAsync(ct);
        return task.Id;
    }
}

public sealed record ApproveBoardTaskCommand(Guid TaskId, string ApprovedBy) : IRequest<bool>;

public sealed class ApproveBoardTaskHandler(
    CeoDbContext db,
    IPublishEndpoint bus) : IRequestHandler<ApproveBoardTaskCommand, bool>
{
    public async Task<bool> Handle(ApproveBoardTaskCommand request, CancellationToken ct)
    {
        var task = await db.BoardTasks.FirstOrDefaultAsync(x => x.Id == request.TaskId, ct);
        if (task is null) return false;
        if (task.Status is BoardTaskStatus.Approved or BoardTaskStatus.Dispatched or BoardTaskStatus.InProgress or BoardTaskStatus.Completed)
            return true;

        task.Status = BoardTaskStatus.Approved;
        task.ApprovedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await bus.Publish(new TaskApprovedEvent(
            task.Id,
            task.Title,
            task.Description,
            task.TargetDepartment,
            task.Priority,
            task.ApprovedAtUtc.Value,
            request.ApprovedBy), ct);

        task.Status = BoardTaskStatus.Dispatched;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record ListBoardTasksQuery() : IRequest<IReadOnlyList<BoardTask>>;

public sealed class ListBoardTasksHandler(CeoDbContext db) : IRequestHandler<ListBoardTasksQuery, IReadOnlyList<BoardTask>>
{
    public async Task<IReadOnlyList<BoardTask>> Handle(ListBoardTasksQuery request, CancellationToken ct)
        => await db.BoardTasks.OrderByDescending(t => t.CreatedAtUtc).Take(200).ToListAsync(ct);
}

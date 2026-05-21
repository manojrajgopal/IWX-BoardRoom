using IWX.CeoAgent.Application.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IWX.CeoAgent.Api.Endpoints;

public static class BoardTaskEndpoints
{
    public static IEndpointRouteBuilder MapBoardTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/tasks").WithTags("BoardTasks");

        g.MapGet("/", async (IMediator m) => Results.Ok(await m.Send(new ListBoardTasksQuery())));

        g.MapPost("/", async (CreateBoardTaskCommand cmd, IMediator m) =>
        {
            var id = await m.Send(cmd);
            return Results.Created($"/tasks/{id}", new { id });
        });

        g.MapPost("/{id:guid}/approve", async (Guid id, [FromQuery] string? approvedBy, IMediator m) =>
        {
            var ok = await m.Send(new ApproveBoardTaskCommand(id, approvedBy ?? "ceo"));
            return ok ? Results.Ok(new { id, approved = true }) : Results.NotFound();
        });

        return app;
    }
}

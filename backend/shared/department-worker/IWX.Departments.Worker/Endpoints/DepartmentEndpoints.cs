using IWX.Contracts.Departments;
using IWX.Departments.Worker.Domain;
using IWX.Departments.Worker.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace IWX.Departments.Worker.Endpoints;

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentEndpoints(
        this IEndpointRouteBuilder app,
        DepartmentDescriptor department)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            service = $"{department.Key}-agent-service",
            department = department.Key,
            displayName = department.DisplayName
        }));

        app.MapGet("/department", () => Results.Ok(department));

        app.MapGet("/tasks", async (DepartmentDbContext db) =>
        {
            var tasks = await db.Tasks
                .OrderByDescending(t => t.ReceivedAtUtc)
                .Take(200)
                .ToListAsync();
            return Results.Ok(tasks);
        });

        app.MapGet("/tasks/{id:guid}", async (Guid id, DepartmentDbContext db) =>
        {
            var task = await db.Tasks.FindAsync(id);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        app.MapGet("/stats", async (DepartmentDbContext db) =>
        {
            var total = await db.Tasks.CountAsync();
            var completed = await db.Tasks.CountAsync(t => t.Status == DepartmentTaskStatus.Completed);
            var failed = await db.Tasks.CountAsync(t => t.Status == DepartmentTaskStatus.Failed);
            var inFlight = await db.Tasks.CountAsync(t =>
                t.Status == DepartmentTaskStatus.Received ||
                t.Status == DepartmentTaskStatus.Processing);
            return Results.Ok(new { department = department.Key, total, completed, failed, inFlight });
        });

        return app;
    }
}

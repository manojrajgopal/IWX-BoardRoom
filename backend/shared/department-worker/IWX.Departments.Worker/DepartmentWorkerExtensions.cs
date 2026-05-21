using IWX.Common.Observability;
using IWX.Contracts.Departments;
using IWX.Contracts.Events;
using IWX.Departments.Worker.Brain;
using IWX.Departments.Worker.Endpoints;
using IWX.Departments.Worker.Infrastructure;
using IWX.Departments.Worker.Messaging;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IWX.Departments.Worker;

/// <summary>
/// One-liner setup for any department agent service.
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// var app = builder.AddIwxDepartmentService(DepartmentRegistry.Hr).Build();
/// app.UseIwxDepartmentService(DepartmentRegistry.Hr);
/// app.Run();
/// </code>
/// </summary>
public static class DepartmentWorkerExtensions
{
    public static WebApplicationBuilder AddIwxDepartmentService(
        this WebApplicationBuilder builder,
        DepartmentDescriptor department,
        Action<IServiceCollection>? configureBrain = null)
    {
        builder.AddIwxObservability($"iwx.{department.Key}-agent-service");

        builder.Services.AddDbContext<DepartmentDbContext>(o =>
            o.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

        builder.Services.AddSingleton(department);

        if (configureBrain is not null)
        {
            configureBrain(builder.Services);
        }
        else
        {
            // Phase 3 default: delegate to AI substrate (reasoning-engine + memory-engine).
            // Falls back to DefaultDepartmentBrain if the substrate is unreachable.
            var opts = new SubstrateOptions
            {
                ReasoningEngineUrl = builder.Configuration["Substrate:ReasoningEngineUrl"] ?? "http://reasoning-engine:8105",
                MemoryEngineUrl = builder.Configuration["Substrate:MemoryEngineUrl"] ?? "http://memory-engine:8100"
            };
            builder.Services.AddSingleton(opts);
            builder.Services.AddHttpClient<IDepartmentBrain, SubstrateDepartmentBrain>(c =>
            {
                c.Timeout = TimeSpan.FromMinutes(5);
            });
        }

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<DepartmentTaskApprovedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                var rmq = builder.Configuration.GetSection("RabbitMq");
                cfg.Host(rmq["Host"] ?? "localhost", h =>
                {
                    h.Username(rmq["User"] ?? "guest");
                    h.Password(rmq["Pass"] ?? "guest");
                });

                cfg.Message<TaskApprovedEvent>(m => m.SetEntityName(Queues.TaskApproved));
                cfg.Message<TaskCompletedEvent>(m => m.SetEntityName(Queues.TaskCompleted));
                cfg.Message<AgentThinkingEvent>(m => m.SetEntityName(Queues.AgentThinking));

                // Each dept gets its own private queue bound to the approved fanout
                cfg.ReceiveEndpoint($"{department.Key}.task.approved", e =>
                {
                    e.ConfigureConsumer<DepartmentTaskApprovedConsumer>(ctx);
                });
            });
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
            .WithOrigins("http://localhost:4200", "http://localhost:8080")
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        return builder;
    }

    public static WebApplication UseIwxDepartmentService(
        this WebApplication app,
        DepartmentDescriptor department)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DepartmentDbContext>();
            var attempts = 0;
            while (attempts++ < 20)
            {
                try { db.Database.EnsureCreated(); break; }
                catch { Thread.Sleep(2000); }
            }
        }

        app.UseCors();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapDepartmentEndpoints(department);
        return app;
    }
}

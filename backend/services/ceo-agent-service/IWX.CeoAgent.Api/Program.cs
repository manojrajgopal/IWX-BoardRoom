using IWX.CeoAgent.Api.Endpoints;
using IWX.CeoAgent.Application.Tasks;
using IWX.CeoAgent.Infrastructure;
using IWX.CeoAgent.Messaging;
using IWX.CeoAgent.Realtime;
using IWX.Common.Observability;
using IWX.Contracts.Departments;
using IWX.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxObservability("iwx.ceo-agent-service");

builder.Services.AddDbContext<CeoDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateBoardTaskCommand>());

builder.Services.AddSignalR();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TaskCompletedConsumer>();
    x.AddConsumer<AgentThinkingConsumer>();

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

        cfg.ReceiveEndpoint(Queues.TaskCompleted, e =>
        {
            e.ConfigureConsumer<TaskCompletedConsumer>(ctx);
        });
        cfg.ReceiveEndpoint(Queues.AgentThinking, e =>
        {
            e.ConfigureConsumer<AgentThinkingConsumer>(ctx);
        });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200", "http://localhost:8080")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CeoDbContext>();
    var attempts = 0;
    while (attempts++ < 20)
    {
        try { await db.Database.EnsureCreatedAsync(); break; }
        catch { await Task.Delay(2000); }
    }
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ceo-agent-service" }));
app.MapBoardTaskEndpoints();
app.MapHub<BoardroomHub>("/hubs/boardroom");

app.Run();

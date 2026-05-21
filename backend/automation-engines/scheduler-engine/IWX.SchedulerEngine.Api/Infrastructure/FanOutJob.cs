using System.Text.Json;
using IWX.Contracts.Automation;
using IWX.SchedulerEngine.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace IWX.SchedulerEngine.Infrastructure;

[DisallowConcurrentExecution]
public sealed class FanOutJob : IJob
{
    private readonly IServiceProvider _sp;
    public FanOutJob(IServiceProvider sp) { _sp = sp; }

    public async Task Execute(IJobExecutionContext context)
    {
        var key = context.JobDetail.Key.Name;
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Key == key, context.CancellationToken);
        if (job is null || !job.Enabled) return;

        Dictionary<string, string> payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, string>>(job.PayloadJson)
                      ?? new Dictionary<string, string>();
        }
        catch { payload = new(); }

        job.LastFiredAtUtc = DateTime.UtcNow;
        job.FireCount += 1;
        await db.SaveChangesAsync(context.CancellationToken);

        await bus.Publish(new SchedulerTickEvent(
            job.Key, job.Name, job.TargetDepartment, payload, DateTime.UtcNow),
            context.CancellationToken);
    }
}

public static class JobRegistrar
{
    public static async Task RegisterAsync(IScheduler scheduler, ScheduledJob job, CancellationToken ct)
    {
        var jobKey = new JobKey(job.Key);
        await scheduler.DeleteJob(jobKey, ct);

        if (!job.Enabled) return;

        var detail = JobBuilder.Create<FanOutJob>()
            .WithIdentity(jobKey)
            .WithDescription(job.Name)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trig-{job.Key}")
            .WithCronSchedule(job.CronExpression)
            .Build();

        await scheduler.ScheduleJob(detail, trigger, ct);
    }

    public static async Task UnregisterAsync(IScheduler scheduler, string key, CancellationToken ct)
    {
        await scheduler.DeleteJob(new JobKey(key), ct);
    }
}

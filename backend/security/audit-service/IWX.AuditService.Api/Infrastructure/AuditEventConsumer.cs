using IWX.AuditService.Domain;
using IWX.Contracts.Security;
using MassTransit;

namespace IWX.AuditService.Infrastructure;

public sealed class AuditRecordedConsumer : IConsumer<AuditRecordedEvent>
{
    private readonly AuditStore _store;
    public AuditRecordedConsumer(AuditStore store) { _store = store; }

    public async Task Consume(ConsumeContext<AuditRecordedEvent> ctx)
    {
        var e = ctx.Message;
        await _store.AppendAsync(new AuditRecord
        {
            Id = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id,
            Actor = e.Actor,
            Action = e.Action,
            Resource = e.Resource,
            Outcome = e.Outcome,
            PayloadJson = e.PayloadJson,
            Source = "bus",
            RecordedAtUtc = e.RecordedAtUtc == default ? DateTime.UtcNow : e.RecordedAtUtc
        }, ctx.CancellationToken);
    }
}

public sealed class ThreatDetectedConsumer : IConsumer<ThreatDetectedEvent>
{
    private readonly AuditStore _store;
    public ThreatDetectedConsumer(AuditStore store) { _store = store; }

    public Task Consume(ConsumeContext<ThreatDetectedEvent> ctx)
    {
        var e = ctx.Message;
        return _store.AppendAsync(new AuditRecord
        {
            Actor = e.Subject,
            Action = $"threat.{e.Category}",
            Resource = e.Source,
            Outcome = e.Severity.ToString(),
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { e.Reason, e.Metadata }),
            Source = "security",
            RecordedAtUtc = e.DetectedAtUtc
        }, ctx.CancellationToken);
    }
}

public sealed class AuthIssuedConsumer : IConsumer<AuthIssuedEvent>
{
    private readonly AuditStore _store;
    public AuthIssuedConsumer(AuditStore store) { _store = store; }

    public Task Consume(ConsumeContext<AuthIssuedEvent> ctx)
    {
        var e = ctx.Message;
        return _store.AppendAsync(new AuditRecord
        {
            Actor = e.Subject,
            Action = "auth.issued",
            Resource = e.TenantId,
            Outcome = e.TokenId,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { e.Roles, e.ExpiresAtUtc }),
            Source = "auth",
            RecordedAtUtc = e.IssuedAtUtc
        }, ctx.CancellationToken);
    }
}

public sealed class AccessDeniedConsumer : IConsumer<AccessDeniedEvent>
{
    private readonly AuditStore _store;
    public AccessDeniedConsumer(AuditStore store) { _store = store; }

    public Task Consume(ConsumeContext<AccessDeniedEvent> ctx)
    {
        var e = ctx.Message;
        return _store.AppendAsync(new AuditRecord
        {
            Actor = e.Subject,
            Action = "access.denied",
            Resource = e.Resource,
            Outcome = e.Action,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { e.Reason }),
            Source = "auth",
            RecordedAtUtc = e.DeniedAtUtc
        }, ctx.CancellationToken);
    }
}

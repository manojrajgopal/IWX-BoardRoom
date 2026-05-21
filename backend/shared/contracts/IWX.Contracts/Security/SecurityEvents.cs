namespace IWX.Contracts.Security;

/// <summary>
/// Stable keys for every security service in the IWX Boardroom.
/// Used for routing, queue names, and docker hostnames.
/// </summary>
public static class SecurityServices
{
    public const string JavaSecurityEngine = "java-security-engine";
    public const string AuthService = "auth-service";
    public const string AuditService = "audit-service";
}

public sealed record SecurityServiceDescriptor(
    string Key,
    string DisplayName,
    string Stack,
    int HttpPort,
    string ServiceHost,
    string Icon);

public static class SecurityRegistry
{
    public static readonly SecurityServiceDescriptor JavaSecurityEngine = new(
        SecurityServices.JavaSecurityEngine,
        "Java Security Engine",
        "Spring Boot",
        8400,
        "java-security-engine",
        "pi-shield");

    public static readonly SecurityServiceDescriptor AuthService = new(
        SecurityServices.AuthService,
        "Auth Service",
        ".NET 10 / JWT+RBAC",
        8401,
        "auth-service",
        "pi-key");

    public static readonly SecurityServiceDescriptor AuditService = new(
        SecurityServices.AuditService,
        "Audit Service",
        ".NET 10 / Mongo+Kafka",
        8402,
        "audit-service",
        "pi-file-edit");

    public static readonly IReadOnlyList<SecurityServiceDescriptor> All = new[]
    {
        JavaSecurityEngine, AuthService, AuditService
    };
}

public static class SecurityQueues
{
    /// <summary>A prompt or behavior was flagged by the security engine.</summary>
    public const string ThreatDetected = "iwx.security.threat.detected";
    /// <summary>A user/agent has been authenticated and a token issued.</summary>
    public const string AuthIssued = "iwx.security.auth.issued";
    /// <summary>An access-denied event from any service (audit-relevant).</summary>
    public const string AccessDenied = "iwx.security.access.denied";
    /// <summary>An append-only audit record (consumed by audit-service from all of iwx.#).</summary>
    public const string AuditRecorded = "iwx.security.audit.recorded";
}

public enum ThreatSeverity { Info, Low, Medium, High, Critical }

public sealed record ThreatDetectedEvent(
    Guid Id,
    string Source,
    string Subject,
    ThreatSeverity Severity,
    string Category,
    string Reason,
    Dictionary<string, string> Metadata,
    DateTime DetectedAtUtc);

public sealed record AuthIssuedEvent(
    string Subject,
    string TenantId,
    IReadOnlyList<string> Roles,
    string TokenId,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc);

public sealed record AccessDeniedEvent(
    string Subject,
    string Resource,
    string Action,
    string Reason,
    DateTime DeniedAtUtc);

public sealed record AuditRecordedEvent(
    Guid Id,
    string Actor,
    string Action,
    string Resource,
    string? Outcome,
    string? PayloadJson,
    DateTime RecordedAtUtc);

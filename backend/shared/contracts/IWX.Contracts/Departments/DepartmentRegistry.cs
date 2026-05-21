namespace IWX.Contracts.Departments;

/// <summary>
/// Authoritative registry for every department service in the IWX Boardroom.
/// Adding a new department = adding one entry here + scaffolding its service.
/// </summary>
public sealed record DepartmentDescriptor(
    string Key,          // routing key (e.g. "hr")
    string DisplayName,  // human label
    string DbName,       // SQL Server database name
    int HttpPort,        // container HTTP port
    string ServiceHost,  // docker-compose service hostname
    string Icon);        // PrimeNG icon hint

public static class DepartmentRegistry
{
    public static readonly DepartmentDescriptor Ceo = new(
        Departments.Ceo, "CEO", "IwxCeo", 8081, "ceo-agent-service", "pi-crown");

    public static readonly DepartmentDescriptor Hr = new(
        Departments.Hr, "Human Resources", "IwxHr", 8082, "hr-agent-service", "pi-users");

    public static readonly DepartmentDescriptor Sales = new(
        Departments.Sales, "Sales", "IwxSales", 8083, "sales-agent-service", "pi-chart-line");

    public static readonly DepartmentDescriptor Finance = new(
        Departments.Finance, "Finance", "IwxFinance", 8084, "finance-agent-service", "pi-wallet");

    public static readonly DepartmentDescriptor Marketing = new(
        Departments.Marketing, "Marketing", "IwxMarketing", 8085, "marketing-agent-service", "pi-megaphone");

    public static readonly DepartmentDescriptor Operations = new(
        Departments.Operations, "Operations", "IwxOperations", 8086, "operations-agent-service", "pi-cog");

    public static readonly DepartmentDescriptor Development = new(
        Departments.Development, "Development", "IwxDevelopment", 8087, "development-agent-service", "pi-code");

    public static readonly DepartmentDescriptor Research = new(
        Departments.Research, "Research", "IwxResearch", 8088, "research-agent-service", "pi-search");

    public static readonly DepartmentDescriptor Legal = new(
        Departments.Legal, "Legal", "IwxLegal", 8089, "legal-agent-service", "pi-shield");

    public static readonly DepartmentDescriptor SocialMedia = new(
        Departments.SocialMedia, "Social Media", "IwxSocialMedia", 8090, "social-media-agent-service", "pi-share-alt");

    public static readonly DepartmentDescriptor Analytics = new(
        Departments.Analytics, "Analytics", "IwxAnalytics", 8091, "analytics-agent-service", "pi-chart-bar");

    public static readonly DepartmentDescriptor CustomerSupport = new(
        Departments.CustomerSupport, "Customer Support", "IwxCustomerSupport", 8092, "customer-support-agent-service", "pi-comments");

    public static readonly DepartmentDescriptor Automation = new(
        Departments.Automation, "Automation", "IwxAutomation", 8093, "automation-agent-service", "pi-bolt");

    public static readonly DepartmentDescriptor PlatformIntelligence = new(
        Departments.PlatformIntelligence, "Platform Intelligence", "IwxPlatformIntelligence", 8094, "platform-intelligence-agent-service", "pi-sparkles");

    public static readonly IReadOnlyList<DepartmentDescriptor> All = new[]
    {
        Ceo, Hr, Sales, Finance, Marketing, Operations, Development,
        Research, Legal, SocialMedia, Analytics, CustomerSupport,
        Automation, PlatformIntelligence
    };

    /// <summary>All department descriptors except CEO (CEO is the orchestrator board, not a worker dept).</summary>
    public static readonly IReadOnlyList<DepartmentDescriptor> Workers =
        All.Where(d => d.Key != Departments.Ceo).ToArray();

    public static DepartmentDescriptor ByKey(string key) =>
        All.FirstOrDefault(d => d.Key == key)
        ?? throw new ArgumentException($"Unknown department key '{key}'", nameof(key));
}

namespace IWX.Departments.Worker.Domain;

public enum DepartmentTaskStatus
{
    Received = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public sealed class DepartmentTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DepartmentTaskStatus Status { get; set; }
    public string? ResultSummary { get; set; }
    public string? ResultPayloadJson { get; set; }
    public string? ErrorMessage { get; set; }
}

namespace IWX.CeoAgent.Domain;

public enum BoardTaskStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Dispatched = 3,
    InProgress = 4,
    Completed = 5,
    Failed = 6,
    Rejected = 7
}

public sealed class BoardTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetDepartment { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public BoardTaskStatus Status { get; set; } = BoardTaskStatus.Draft;
    public string CreatedBy { get; set; } = "ceo";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ResultSummary { get; set; }
    public string? ResultPayloadJson { get; set; }
}

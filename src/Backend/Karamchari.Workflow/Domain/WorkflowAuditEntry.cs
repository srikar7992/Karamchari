namespace Karamchari.Workflow.Domain;

public sealed record WorkflowAuditEntry
{
    public string Action { get; init; } = string.Empty;
    public Guid ActorId { get; init; }
    public int? StepOrder { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string StateSnapshot { get; init; } = string.Empty;
}

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record WorkflowAuditEntry
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Action { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ActorId { get; init; }
    public int? StepOrder { get; init; }
    public string? Notes { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string StateSnapshot { get; init; } = string.Empty;
}

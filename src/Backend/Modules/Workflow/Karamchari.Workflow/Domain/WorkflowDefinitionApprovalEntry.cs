namespace Karamchari.Workflow.Domain;

/// <summary>
/// Immutable record of a single policy lifecycle transition on a <see cref="WorkflowDefinition"/>.
/// Answers the regulatory audit question: who changed a policy rule, when, and why.
///
/// Example: a Bonus > 20% compensation rule transitions from Draft → Published.
/// The regulator can query the approval history to see which HR administrator
/// approved the rule, when they approved it, and what justification was provided.
/// </summary>
public sealed class WorkflowDefinitionApprovalEntry
{
    // EF Core requires a parameterless constructor for owned entity materialization.
    private WorkflowDefinitionApprovalEntry() { }

    public WorkflowDefinitionApprovalEntry(
        Guid id,
        string actorId,
        PolicyStatus fromStatus,
        PolicyStatus toStatus,
        DateTimeOffset occurredAt,
        string? notes = null)
    {
        Id = id;
        ActorId = actorId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        OccurredAt = occurredAt;
        Notes = notes;
    }

    /// <summary>Surrogate key for EF owned-entity materialization.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identity of the actor who triggered the transition.
    /// Typically a user ID from the JWT sub claim. Stored as string to remain
    /// identity-provider agnostic (Guid, email, or opaque sub are all valid).
    /// </summary>
    public string ActorId { get; private set; } = string.Empty;

    /// <summary>Status before the transition.</summary>
    public PolicyStatus FromStatus { get; private set; }

    /// <summary>Status after the transition.</summary>
    public PolicyStatus ToStatus { get; private set; }

    /// <summary>Wall-clock UTC timestamp when the transition occurred.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Optional justification supplied by the actor.
    /// Regulators may require a business reason for Published → Deprecated transitions.
    /// </summary>
    public string? Notes { get; private set; }
}

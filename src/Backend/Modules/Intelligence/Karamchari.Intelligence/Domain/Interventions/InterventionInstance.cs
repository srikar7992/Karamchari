// -----------------------------------------------------------------------
// <copyright file="InterventionInstance.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Workforce;

namespace Karamchari.Intelligence.Domain.Interventions;

/// <summary>
/// Lifecycle states of an intervention that is being executed.
/// </summary>
public enum InterventionInstanceStatus
{
    /// <summary>Recommendation surfaced; waiting for the owner to acknowledge and start.</summary>
    Pending,
    /// <summary>Owner has explicitly accepted responsibility for this intervention.</summary>
    Accepted,
    /// <summary>Intervention actively in progress (owner started the workflow).</summary>
    InProgress,
    /// <summary>Intervention completed; outcome evaluation window begins.</summary>
    Completed,
    /// <summary>Intervention abandoned before completion.</summary>
    Cancelled
}

/// <summary>
/// Represents a specific execution of an <see cref="InterventionTemplate"/> for an employee.
/// Bridges "what should be done" (WorkforceRecommendation) to "what was actually done."
///
/// Lifecycle: Pending → Accepted → InProgress → Completed | Cancelled
///
/// When <see cref="Complete"/> is called, the linked WorkforceRecommendation is resolved,
/// which triggers InterventionTrackerService to evaluate effectiveness 30 days later.
///
/// Maps to Intel_InterventionInstances.
/// </summary>
public sealed class InterventionInstance : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>The recommendation this instance was created from.</summary>
    public Guid RecommendationId { get; private set; }

    /// <summary>The template being executed (denormalised from the recommendation for query convenience).</summary>
    public Guid TemplateId { get; private set; }

    public Guid EmployeeId { get; private set; }

    /// <summary>Who is responsible for executing this intervention.</summary>
    public InterventionOwnerType OwnerType { get; private set; }

    /// <summary>Specific actor ID (manager ID, HR user ID, etc.).</summary>
    public string? OwnerId { get; private set; }

    public InterventionInstanceStatus Status { get; private set; }

    public DateTime AssignedAt { get; private set; }

    public DateTime? AcceptedAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    /// <summary>Free-text progress notes added during execution.</summary>
    public string? Notes { get; private set; }

    /// <summary>What the owner observed at completion — feeds explainability of effectiveness.</summary>
    public string? OutcomeNotes { get; private set; }

    public string? CancellationReason { get; private set; }

    private InterventionInstance() { }

    /// <summary>
    /// Creates a new pending instance from an accepted recommendation.
    /// </summary>
    public static InterventionInstance Create(
        string tenantId,
        Guid recommendationId,
        Guid templateId,
        Guid employeeId,
        InterventionOwnerType ownerType,
        string? ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return new InterventionInstance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecommendationId = recommendationId,
            TemplateId = templateId,
            EmployeeId = employeeId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            Status = InterventionInstanceStatus.Pending,
            AssignedAt = DateTime.UtcNow
        };
    }

    /// <summary>Owner acknowledges responsibility. Pending → Accepted.</summary>
    public void Accept()
    {
        if (Status != InterventionInstanceStatus.Pending)
            throw new InvalidOperationException($"Cannot accept an instance in {Status} state.");

        Status = InterventionInstanceStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
    }

    /// <summary>Owner begins executing the intervention workflow. Accepted → InProgress.</summary>
    public void Start()
    {
        if (Status is not (InterventionInstanceStatus.Pending or InterventionInstanceStatus.Accepted))
            throw new InvalidOperationException($"Cannot start an instance in {Status} state.");

        Status = InterventionInstanceStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        AcceptedAt ??= StartedAt; // implicit accept if owner skipped that step
    }

    /// <summary>
    /// Marks the intervention as completed and records the owner's observations.
    /// InProgress → Completed.
    ///
    /// Callers should also call <c>recommendation.Resolve(ownerId)</c> so that
    /// InterventionTrackerService evaluates effectiveness 30 days later.
    /// </summary>
    public void Complete(string? outcomeNotes = null)
    {
        if (Status is InterventionInstanceStatus.Completed or InterventionInstanceStatus.Cancelled)
            throw new InvalidOperationException($"Cannot complete an instance in {Status} state.");

        Status = InterventionInstanceStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        OutcomeNotes = outcomeNotes;
        StartedAt ??= CompletedAt;
        AcceptedAt ??= CompletedAt;
    }

    /// <summary>Abandons the intervention before completion.</summary>
    public void Cancel(string? reason = null)
    {
        if (Status is InterventionInstanceStatus.Completed or InterventionInstanceStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel an instance in {Status} state.");

        Status = InterventionInstanceStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }

    /// <summary>Appends or replaces progress notes.</summary>
    public void AddNotes(string notes)
    {
        Notes = notes;
    }
}

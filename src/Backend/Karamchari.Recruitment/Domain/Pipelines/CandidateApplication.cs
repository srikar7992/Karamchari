using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Domain.Primitives;

namespace Karamchari.Recruitment.Domain.Pipelines;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ApplicationStatus
{
    /// <inheritdoc/>
    Active,
    /// <inheritdoc/>
    Rejected,
    /// <inheritdoc/>
    Withdrawn,
    /// <inheritdoc/>
    Hired
}

/// <summary>
/// Aggregate root representing a specific application to a JobRequisition.
/// Orchestrates pipeline stage transitions.
/// </summary>
public sealed class CandidateApplication : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CandidateId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RequisitionId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ApplicationStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrentStage { get; private set; } = string.Empty; // e.g., "Screening", "TechnicalInterview"

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CandidateSource Source { get; private set; }
    /// <inheritdoc/>
    public string? ReferrerId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset AppliedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private CandidateApplication() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CandidateApplication Apply(
        string tenantId,
        Guid candidateId,
        Guid requisitionId,
        CandidateSource source,
        string? referrerId = null)
    {
        return new CandidateApplication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CandidateId = candidateId,
            RequisitionId = requisitionId,
            Source = source,
            ReferrerId = referrerId,
            Status = ApplicationStatus.Active,
            CurrentStage = "Applied",
            AppliedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AdvanceStage(string newStage)
    {
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot change stage. Application is {Status}.");

        CurrentStage = newStage;
        // Raise PipelineStageAdvancedEvent
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Reject(string reason)
    {
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot reject application in state {Status}.");

        Status = ApplicationStatus.Rejected;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        // Raise CandidateRejectedEvent
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Withdraw(string reason)
    {
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot withdraw application in state {Status}.");

        Status = ApplicationStatus.Withdrawn;
        ClosedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Hire(decimal finalCompensation, string currency)
    {
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot hire candidate in state {Status}.");

        Status = ApplicationStatus.Hired;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        // Raise HiringDecisionCompletedEvent for HR Onboarding sync
    }
}

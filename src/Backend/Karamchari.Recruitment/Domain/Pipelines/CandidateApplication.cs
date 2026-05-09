using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Domain.Primitives;

namespace Karamchari.Recruitment.Domain.Pipelines;

public enum ApplicationStatus
{
    Active,
    Rejected,
    Withdrawn,
    Hired
}

/// <summary>
/// Aggregate root representing a specific application to a JobRequisition.
/// Orchestrates pipeline stage transitions.
/// </summary>
public sealed class CandidateApplication : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid CandidateId { get; private set; }
    public Guid RequisitionId { get; private set; }
    
    public ApplicationStatus Status { get; private set; }
    public string CurrentStage { get; private set; } = string.Empty; // e.g., "Screening", "TechnicalInterview"
    
    public CandidateSource Source { get; private set; }
    public string? ReferrerId { get; private set; }
    
    public DateTimeOffset AppliedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private CandidateApplication() { }

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

    public void AdvanceStage(string newStage)
    {
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot change stage. Application is {Status}.");

        CurrentStage = newStage;
        // Raise PipelineStageAdvancedEvent
    }

    public void Reject(string reason)
    {
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot reject application in state {Status}.");

        Status = ApplicationStatus.Rejected;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        // Raise CandidateRejectedEvent
    }

    public void Withdraw(string reason)
    {
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot withdraw application in state {Status}.");

        Status = ApplicationStatus.Withdrawn;
        ClosedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Hire(decimal finalCompensation, string currency)
    {
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot hire candidate in state {Status}.");

        Status = ApplicationStatus.Hired;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        // Raise HiringDecisionCompletedEvent for HR Onboarding sync
    }
}

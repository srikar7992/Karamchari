using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Promotions.Events;
using Karamchari.Performance.Domain.Scoring;

namespace Karamchari.Performance.Domain.Promotions;

/// <summary>
/// Drives a promotion recommendation. Coordination is managed by the Workflow module.
/// </summary>
public sealed class PromotionRecommendation : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<PromotionApprovalAction> _approvalActions = [];

    private PromotionRecommendation() { /* EF materialization */ }

    private PromotionRecommendation(
        Guid id,
        string tenantId,
        Guid employeeId,
        Guid nominatedByManagerId,
        string currentGrade,
        string proposedGrade,
        string currentTitle,
        string proposedTitle,
        decimal readinessScore,
        string justification,
        DateOnly proposedEffectiveDate) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        NominatedByManagerId = nominatedByManagerId;
        CurrentGrade = currentGrade;
        ProposedGrade = proposedGrade;
        CurrentTitle = currentTitle;
        ProposedTitle = proposedTitle;
        ReadinessScore = readinessScore;
        Justification = justification;
        ProposedEffectiveDate = proposedEffectiveDate;
        Status = PromotionStatus.Draft;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid NominatedByManagerId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrentGrade { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ProposedGrade { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrentTitle { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ProposedTitle { get; private set; } = string.Empty;

    /// <summary>0â€“100 score computed by IPromotionReadinessEngine. Immutable after creation.</summary>
    public decimal ReadinessScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Justification { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly ProposedEffectiveDate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public PromotionStatus Status { get; private set; }
    public DateOnly? ApprovedEffectiveDate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<PromotionApprovalAction> ApprovalActions => _approvalActions.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static PromotionRecommendation Create(
        string tenantId,
        Guid employeeId,
        Guid nominatedByManagerId,
        string currentGrade,
        string proposedGrade,
        string currentTitle,
        string proposedTitle,
        decimal readinessScore,
        string justification,
        DateOnly proposedEffectiveDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentGrade);
        ArgumentException.ThrowIfNullOrWhiteSpace(proposedGrade);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(proposedTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        if (readinessScore < 0 || readinessScore > 100)
            throw new ArgumentOutOfRangeException(nameof(readinessScore), "ReadinessScore must be 0â€“100.");

        return new PromotionRecommendation(Guid.NewGuid(), tenantId, employeeId, nominatedByManagerId,
            currentGrade.Trim(), proposedGrade.Trim(), currentTitle.Trim(), proposedTitle.Trim(),
            readinessScore, justification.Trim(), proposedEffectiveDate);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Submit()
    {
        if (Status != PromotionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit from {Status}.");

        // Business state remains Draft until finalized by coordination.
    }

    /// <summary>
    /// Finalizes the promotion recommendation, making it authoritative truth.
    /// Called by coordination logic (Workflow) upon successful approval.
    /// </summary>
    public void Finalize(Guid actorId)
    {
        if (Status != PromotionStatus.Draft)
            throw new InvalidOperationException($"Cannot finalize from {Status}.");

        Status = PromotionStatus.Approved;
        ApprovedEffectiveDate = ProposedEffectiveDate;

        RaiseDomainEvent(new PromotionApproved(
            Id, TenantId, EmployeeId, CurrentGrade, ProposedGrade,
            CurrentTitle, ProposedTitle, ApprovedEffectiveDate.Value, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Rejects the promotion recommendation.
    /// </summary>
    public void Reject(Guid actorId, string reason)
    {
        if (Status != PromotionStatus.Draft)
            throw new InvalidOperationException($"Cannot reject from {Status}.");

        Status = PromotionStatus.Rejected;

        RaiseDomainEvent(new PromotionRejected(
            Id, TenantId, EmployeeId, reason, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Withdraw()
    {
        if (Status is PromotionStatus.Approved or PromotionStatus.Rejected)
            throw new InvalidOperationException($"Cannot withdraw a {Status} recommendation.");
        Status = PromotionStatus.Withdrawn;
    }
}

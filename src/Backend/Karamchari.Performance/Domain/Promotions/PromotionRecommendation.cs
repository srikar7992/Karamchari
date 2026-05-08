using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Promotions.Events;
using Karamchari.Performance.Domain.Scoring;

namespace Karamchari.Performance.Domain.Promotions;

/// <summary>
/// Drives a promotion recommendation through a multi-stage approval workflow.
/// Stages: Manager → HR → Leadership (configurable subset at tenant level).
/// ReadinessScore is computed at creation by IPromotionReadinessEngine — immutable after submission.
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

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid NominatedByManagerId { get; private set; }
    public string CurrentGrade { get; private set; } = string.Empty;
    public string ProposedGrade { get; private set; } = string.Empty;
    public string CurrentTitle { get; private set; } = string.Empty;
    public string ProposedTitle { get; private set; } = string.Empty;

    /// <summary>0–100 score computed by IPromotionReadinessEngine. Immutable after creation.</summary>
    public decimal ReadinessScore { get; private set; }
    public string Justification { get; private set; } = string.Empty;
    public DateOnly ProposedEffectiveDate { get; private set; }
    public PromotionStatus Status { get; private set; }
    public DateOnly? ApprovedEffectiveDate { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyList<PromotionApprovalAction> ApprovalActions => _approvalActions.AsReadOnly();

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
            throw new ArgumentOutOfRangeException(nameof(readinessScore), "ReadinessScore must be 0–100.");

        return new PromotionRecommendation(Guid.NewGuid(), tenantId, employeeId, nominatedByManagerId,
            currentGrade.Trim(), proposedGrade.Trim(), currentTitle.Trim(), proposedTitle.Trim(),
            readinessScore, justification.Trim(), proposedEffectiveDate);
    }

    public void Submit()
    {
        if (Status != PromotionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit from {Status}.");
        Status = PromotionStatus.Submitted;
    }

    public void Approve(PromotionApprovalStage stage, Guid actorId, string? comments, bool isLastStage)
    {
        EnsureCanAct();
        _approvalActions.Add(new PromotionApprovalAction(Guid.NewGuid(), Id, stage, actorId, true, comments));

        Status = stage switch
        {
            PromotionApprovalStage.Manager => PromotionStatus.ApprovedByManager,
            PromotionApprovalStage.HR => PromotionStatus.ApprovedByHR,
            PromotionApprovalStage.Leadership => PromotionStatus.ApprovedByLeadership,
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };

        if (isLastStage)
        {
            Status = PromotionStatus.Approved;
            ApprovedEffectiveDate = ProposedEffectiveDate;

            RaiseDomainEvent(new PromotionApproved(
                Id, TenantId, EmployeeId, CurrentGrade, ProposedGrade,
                CurrentTitle, ProposedTitle, ApprovedEffectiveDate.Value, DateTimeOffset.UtcNow));
        }
    }

    public void Reject(PromotionApprovalStage stage, Guid actorId, string reason)
    {
        EnsureCanAct();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        _approvalActions.Add(new PromotionApprovalAction(Guid.NewGuid(), Id, stage, actorId, false, reason));
        Status = PromotionStatus.Rejected;

        RaiseDomainEvent(new PromotionRejected(
            Id, TenantId, EmployeeId, stage, reason, DateTimeOffset.UtcNow));
    }

    public void Withdraw()
    {
        if (Status is PromotionStatus.Approved or PromotionStatus.Rejected)
            throw new InvalidOperationException($"Cannot withdraw a {Status} recommendation.");
        Status = PromotionStatus.Withdrawn;
    }

    private void EnsureCanAct()
    {
        if (Status is PromotionStatus.Approved or PromotionStatus.Rejected or PromotionStatus.Withdrawn)
            throw new InvalidOperationException($"Cannot act on a {Status} recommendation.");
    }
}

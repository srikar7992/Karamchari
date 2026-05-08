using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// Denormalized read model: one row per manager per review cycle.
/// Updated by projection consumers reacting to goal/KPI/review domain events.
/// Never queried via aggregate navigation — direct table scan with tenant filter.
/// </summary>
public sealed class ManagerDashboardProjection : Entity<Guid>, ITenantOwned
{
    private ManagerDashboardProjection() { /* EF materialization */ }

    public string TenantId { get; private set; } = string.Empty;
    public Guid ManagerEmployeeId { get; private set; }
    public Guid ReviewCycleId { get; private set; }
    public string CycleName { get; private set; } = string.Empty;

    // Team goal summary
    public int TotalTeamGoals { get; private set; }
    public int OnTrackGoals { get; private set; }
    public int OffTrackGoals { get; private set; }
    public int OverdueGoals { get; private set; }
    public decimal GoalCompletionRate { get; private set; }

    // Review task summary
    public int TotalReviewsRequired { get; private set; }
    public int ReviewsCompleted { get; private set; }
    public int ReviewsOverdue { get; private set; }
    public int PendingApprovals { get; private set; }

    // KPI summary
    public int KPIsAtRisk { get; private set; }
    public int KPIsOffTrack { get; private set; }

    // Promotion pipeline
    public int PendingPromotionApprovals { get; private set; }

    // Feedback
    public int PendingFeedbackRequests { get; private set; }

    public DateTimeOffset LastRefreshedUtc { get; private set; }

    public static ManagerDashboardProjection Create(
        string tenantId,
        Guid managerEmployeeId,
        Guid reviewCycleId,
        string cycleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cycleName);
        return new ManagerDashboardProjection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ManagerEmployeeId = managerEmployeeId,
            ReviewCycleId = reviewCycleId,
            CycleName = cycleName,
            LastRefreshedUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Refresh(
        int totalTeamGoals, int onTrackGoals, int offTrackGoals, int overdueGoals,
        decimal goalCompletionRate,
        int totalReviewsRequired, int reviewsCompleted, int reviewsOverdue, int pendingApprovals,
        int kpisAtRisk, int kpisOffTrack,
        int pendingPromotionApprovals,
        int pendingFeedbackRequests)
    {
        TotalTeamGoals = totalTeamGoals;
        OnTrackGoals = onTrackGoals;
        OffTrackGoals = offTrackGoals;
        OverdueGoals = overdueGoals;
        GoalCompletionRate = goalCompletionRate;
        TotalReviewsRequired = totalReviewsRequired;
        ReviewsCompleted = reviewsCompleted;
        ReviewsOverdue = reviewsOverdue;
        PendingApprovals = pendingApprovals;
        KPIsAtRisk = kpisAtRisk;
        KPIsOffTrack = kpisOffTrack;
        PendingPromotionApprovals = pendingPromotionApprovals;
        PendingFeedbackRequests = pendingFeedbackRequests;
        LastRefreshedUtc = DateTimeOffset.UtcNow;
    }
}

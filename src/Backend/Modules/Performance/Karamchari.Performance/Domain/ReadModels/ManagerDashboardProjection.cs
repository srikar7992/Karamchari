using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// Denormalized read model: one row per manager per review cycle.
/// Updated by projection consumers reacting to goal/KPI/review domain events.
/// Never queried via aggregate navigation â€” direct table scan with tenant filter.
/// </summary>
public sealed class ManagerDashboardProjection : Entity<Guid>, ITenantOwned
{
    private ManagerDashboardProjection() { /* EF materialization */ }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ManagerEmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ReviewCycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CycleName { get; private set; } = string.Empty;

    // Team goal summary
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalTeamGoals { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int OnTrackGoals { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int OffTrackGoals { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int OverdueGoals { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal GoalCompletionRate { get; private set; }

    // Review task summary
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalReviewsRequired { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int ReviewsCompleted { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int ReviewsOverdue { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int PendingApprovals { get; private set; }

    // KPI summary
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int KPIsAtRisk { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int KPIsOffTrack { get; private set; }

    // Promotion pipeline
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int PendingPromotionApprovals { get; private set; }

    // Feedback
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int PendingFeedbackRequests { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastRefreshedUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

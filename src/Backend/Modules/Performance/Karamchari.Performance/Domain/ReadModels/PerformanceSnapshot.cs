// -----------------------------------------------------------------------
// <copyright file="PerformanceSnapshot.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// Immutable point-in-time performance summary materialized after a review cycle completes calibration.
/// Serves analytics, trend analysis, succession planning, and attrition risk detection.
/// Never updated after creation â€” append a new snapshot if recalibration occurs.
/// </summary>
public sealed class PerformanceSnapshot : Entity<Guid>, ITenantOwned
{
    private PerformanceSnapshot() { /* EF materialization */ }

    private PerformanceSnapshot(
        Guid id,
        string tenantId,
        Guid employeeId,
        Guid reviewCycleId,
        string cycleName,
        DateOnly reviewPeriodStart,
        DateOnly reviewPeriodEnd,
        decimal compositeScore,
        string performanceBucket,
        decimal? goalCompletionRate,
        decimal? okrScore,
        decimal? kpiScore,
        decimal? peerFeedbackScore,
        bool isHighPerformer,
        bool isAtRetentionRisk) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        ReviewCycleId = reviewCycleId;
        CycleName = cycleName;
        ReviewPeriodStart = reviewPeriodStart;
        ReviewPeriodEnd = reviewPeriodEnd;
        CompositeScore = compositeScore;
        PerformanceBucket = performanceBucket;
        GoalCompletionRate = goalCompletionRate;
        OKRScore = okrScore;
        KPIScore = kpiScore;
        PeerFeedbackScore = peerFeedbackScore;
        IsHighPerformer = isHighPerformer;
        IsAtRetentionRisk = isAtRetentionRisk;
        MaterializedOnUtc = DateTimeOffset.UtcNow;
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
    public Guid ReviewCycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CycleName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly ReviewPeriodStart { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly ReviewPeriodEnd { get; private set; }

    /// <summary>Weighted composite 0â€“100 across all scoring dimensions.</summary>
    public decimal CompositeScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PerformanceBucket { get; private set; } = string.Empty;
    public decimal? GoalCompletionRate { get; private set; }
    public decimal? OKRScore { get; private set; }
    public decimal? KPIScore { get; private set; }
    public decimal? PeerFeedbackScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsHighPerformer { get; private set; }

    /// <summary>Derived from composite score trajectory + engagement signals. Used in succession planning.</summary>
    public bool IsAtRetentionRisk { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset MaterializedOnUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static PerformanceSnapshot Materialize(
        string tenantId,
        Guid employeeId,
        Guid reviewCycleId,
        string cycleName,
        DateOnly reviewPeriodStart,
        DateOnly reviewPeriodEnd,
        decimal compositeScore,
        string performanceBucket,
        decimal? goalCompletionRate = null,
        decimal? okrScore = null,
        decimal? kpiScore = null,
        decimal? peerFeedbackScore = null,
        bool isHighPerformer = false,
        bool isAtRetentionRisk = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cycleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(performanceBucket);

        return new PerformanceSnapshot(Guid.NewGuid(), tenantId, employeeId, reviewCycleId, cycleName,
            reviewPeriodStart, reviewPeriodEnd, compositeScore, performanceBucket,
            goalCompletionRate, okrScore, kpiScore, peerFeedbackScore,
            isHighPerformer, isAtRetentionRisk);
    }
}

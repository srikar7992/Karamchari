namespace Karamchari.Performance.Domain.Scoring;

/// <summary>
/// Computes a promotion readiness score (0–100) from multiple data signals.
/// Weighted composite: PerformanceHistory(30%) + KPIAchievement(20%) +
/// SkillGapCoverage(25%) + TenureAtGrade(10%) + PeerFeedback(15%).
/// Returns 0 if any PIP entry exists in the last 2 cycles.
/// </summary>
public interface IPromotionReadinessEngine
{
    Task<PromotionReadinessResult> ComputeAsync(
        string tenantId,
        Guid employeeId,
        string currentGrade,
        CancellationToken cancellationToken = default);
}

public sealed record PromotionReadinessResult(
    decimal Score,
    decimal PerformanceHistoryScore,
    decimal KpiAchievementScore,
    decimal SkillGapCoverageScore,
    decimal TenureScore,
    decimal PeerFeedbackScore,
    IReadOnlyList<PromotionRiskIndicator> RiskIndicators);

public sealed record PromotionRiskIndicator(
    string Code,
    string Description,
    PromotionRiskSeverity Severity);

public enum PromotionRiskSeverity
{
    Info = 1,
    Warning = 2,
    Blocker = 3
}

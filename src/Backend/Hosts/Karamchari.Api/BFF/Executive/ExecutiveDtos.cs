namespace Karamchari.Api.BFF.Executive;

// â”€â”€ Org Performance Summary â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record OrgPerformanceSummaryResponse(
    string CycleName,
    int TotalEmployees,
    int EvaluatedEmployees,
    decimal EvaluationCoveragePercent,
    PerformanceDistributionDto Distribution,
    SuccessionSummaryDto Succession,
    RetentionRiskSummaryDto RetentionRisk,
    DateTimeOffset DataAsOf,
    bool IsStale);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record PerformanceDistributionDto(
    int HighPerformers,
    int MediumPerformers,
    int LowPerformers,
    decimal HighPerformerPercent);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record SuccessionSummaryDto(
    int SuccessionCandidates,
    int PromotionReady,
    int HighPotentialHigh);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record RetentionRiskSummaryDto(
    int AtRetentionRisk,
    decimal RetentionRiskPercent);

// â”€â”€ Org Talent Heatmap â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record OrgTalentHeatmapResponse(
    string CycleName,
    IReadOnlyList<OrgHeatmapEntry> Entries,
    NineBoxDistributionDto NineBoxDistribution,
    DateTimeOffset DataAsOf);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record OrgHeatmapEntry(
    Guid EmployeeId,
    string DisplayName,
    string Department,
    string CareerLevel,
    string NineBoxPosition,
    decimal CompositeScore,
    bool IsHighPerformer,
    bool IsAtRetentionRisk,
    bool IsPromotionReady,
    bool IsSuccessionCandidate);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record NineBoxDistributionDto(
    int HighHigh,
    int HighMedium,
    int HighLow,
    int MediumHigh,
    int MediumMedium,
    int MediumLow,
    int LowHigh,
    int LowMedium,
    int LowLow);

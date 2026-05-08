namespace Karamchari.Api.BFF.Executive;

// ── Org Performance Summary ───────────────────────────────────────────────────

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

public sealed record PerformanceDistributionDto(
    int HighPerformers,
    int MediumPerformers,
    int LowPerformers,
    decimal HighPerformerPercent);

public sealed record SuccessionSummaryDto(
    int SuccessionCandidates,
    int PromotionReady,
    int HighPotentialHigh);

public sealed record RetentionRiskSummaryDto(
    int AtRetentionRisk,
    decimal RetentionRiskPercent);

// ── Org Talent Heatmap ────────────────────────────────────────────────────────

public sealed record OrgTalentHeatmapResponse(
    string CycleName,
    IReadOnlyList<OrgHeatmapEntry> Entries,
    NineBoxDistributionDto NineBoxDistribution,
    DateTimeOffset DataAsOf);

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

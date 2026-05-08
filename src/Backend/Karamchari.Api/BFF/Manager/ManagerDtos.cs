namespace Karamchari.Api.BFF.Manager;

// ── Dashboard ─────────────────────────────────────────────────────────────────

public sealed record ManagerDashboardResponse(
    string CycleName,
    GoalSummaryWidget Goals,
    ReviewSummaryWidget Reviews,
    KpiSummaryWidget Kpis,
    int PendingPromotionApprovals,
    int PendingFeedbackRequests,
    DateTimeOffset DataAsOf,
    bool IsStale);

public sealed record GoalSummaryWidget(
    int Total,
    int OnTrack,
    int OffTrack,
    int Overdue,
    decimal CompletionRate);

public sealed record ReviewSummaryWidget(
    int Required,
    int Completed,
    int Overdue,
    int PendingApprovals);

public sealed record KpiSummaryWidget(int AtRisk, int OffTrack);

// ── Review Inbox ──────────────────────────────────────────────────────────────

public sealed record ReviewInboxPageResponse(
    IReadOnlyList<ReviewInboxItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record ReviewInboxItem(
    Guid AssignmentId,
    Guid RevieweeEmployeeId,
    string RevieweeDisplayName,
    string CycleName,
    string ReviewerRole,
    DateTimeOffset Deadline,
    bool IsOverdue,
    int HoursUntilDeadline,
    int Priority);

// ── Team Goals ────────────────────────────────────────────────────────────────

public sealed record TeamGoalsResponse(
    string CycleName,
    string Department,
    decimal CompletionRate,
    decimal AverageProgress,
    int TotalGoals,
    int CompletedGoals,
    int InProgressGoals,
    int OffTrackGoals,
    int NotStartedGoals,
    int TeamSize,
    int EmployeesWithNoGoals,
    DateTimeOffset DataAsOf,
    bool IsStale);

// ── Promotion Pipeline ────────────────────────────────────────────────────────

public sealed record PromotionPipelinePageResponse(
    IReadOnlyList<PromotionPipelineEntry> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record PromotionPipelineEntry(
    Guid RecommendationId,
    Guid EmployeeId,
    string EmployeeDisplayName,
    string CurrentLevel,
    string ProposedLevel,
    string Department,
    string Status,
    string CurrentStage,
    decimal ReadinessScore,
    int DaysInCurrentStage,
    bool IsStale);

// ── Talent Heatmap ────────────────────────────────────────────────────────────

public sealed record TalentHeatmapResponse(
    string CycleName,
    IReadOnlyList<TalentHeatmapItem> Entries,
    DateTimeOffset DataAsOf);

public sealed record TalentHeatmapItem(
    Guid EmployeeId,
    string DisplayName,
    string Department,
    string CareerLevel,
    string NineBoxPosition,
    decimal CompositeScore,
    int PotentialRating,
    bool IsHighPerformer,
    bool IsAtRetentionRisk,
    bool IsPromotionReady,
    bool IsSuccessionCandidate);

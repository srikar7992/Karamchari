namespace Karamchari.Api.BFF.HR;

// ── Review Cycles ─────────────────────────────────────────────────────────────

public sealed record ReviewCyclePageResponse(
    IReadOnlyList<ReviewCycleDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record ReviewCycleDto(
    Guid CycleId,
    string Name,
    string Type,
    string Status,
    DateOnly ReviewPeriodStart,
    DateOnly ReviewPeriodEnd,
    DateOnly SubmissionDeadline,
    int TotalAssignments,
    int CompletedAssignments,
    decimal CompletionRate);

// ── Calibration Board ─────────────────────────────────────────────────────────

public sealed record CalibrationBoardResponse(
    IReadOnlyList<CalibrationSessionDto> Sessions,
    int TotalSessions,
    int SessionsPendingFinalization);

public sealed record CalibrationSessionDto(
    Guid SessionId,
    Guid ReviewCycleId,
    string SessionName,
    string Status,
    int TotalEntries,
    int TopPerformerCount,
    int HighPerformerCount,
    int MeetsExpectationsCount,
    int BelowExpectationsCount,
    int UnderPerformerCount,
    int TotalPanelMembers,
    int PanelMembersJoined,
    int AdjustmentsMade,
    int EntriesPendingFinalization,
    DateTimeOffset? FinalizedOnUtc,
    DateTimeOffset LastRefreshedUtc);

// ── Promotion Approvals ───────────────────────────────────────────────────────

public sealed record HRPromotionPipelineResponse(
    IReadOnlyList<HRPromotionEntry> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record HRPromotionEntry(
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

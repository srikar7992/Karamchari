namespace Karamchari.Api.BFF.Employee;

// ── My Goals ──────────────────────────────────────────────────────────────────

public sealed record MyGoalsResponse(
    IReadOnlyList<MyGoalItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record MyGoalItem(
    Guid GoalId,
    Guid CycleId,
    string Title,
    string? Description,
    string Type,
    decimal TargetValue,
    string? Unit,
    decimal CurrentProgress,
    decimal? FinalScore,
    string Status,
    decimal Weight);

// ── My Reviews ────────────────────────────────────────────────────────────────

public sealed record MyReviewsResponse(
    IReadOnlyList<ReviewInboxItemDto> Pending,
    int PendingCount);

public sealed record ReviewInboxItemDto(
    Guid AssignmentId,
    Guid RevieweeEmployeeId,
    string RevieweeDisplayName,
    string CycleName,
    string ReviewerRole,
    DateTimeOffset Deadline,
    bool IsOverdue,
    int Priority);

// ── My Skills ────────────────────────────────────────────────────────────────

public sealed record MySkillsResponse(
    IReadOnlyList<SkillInventoryItemDto> Items,
    int SkillsWithGap);

public sealed record SkillInventoryItemDto(
    Guid SkillId,
    string SkillName,
    string Category,
    string Domain,
    string CurrentProficiency,
    string? TargetProficiency,
    int? ProficiencyGap,
    DateTimeOffset LastAssessedOn);

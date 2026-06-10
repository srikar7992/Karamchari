// -----------------------------------------------------------------------
// <copyright file="HrDtos.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.BFF.HR;

// â”€â”€ Review Cycles â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ReviewCyclePageResponse(
    IReadOnlyList<ReviewCycleDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

// â”€â”€ Calibration Board â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record CalibrationBoardResponse(
    IReadOnlyList<CalibrationSessionDto> Sessions,
    int TotalSessions,
    int SessionsPendingFinalization);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

// â”€â”€ Promotion Approvals â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record HRPromotionPipelineResponse(
    IReadOnlyList<HRPromotionEntry> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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
